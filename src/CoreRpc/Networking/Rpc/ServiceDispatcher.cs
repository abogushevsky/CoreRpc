using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc.Exceptions;
using CoreRpc.Networking.Rpc.ServiceAnnotations;
using CoreRpc.Serialization;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.Rpc
{
	internal class ServiceDispatcher<TService>
	{
		public ServiceDispatcher(ISerializerFactory serializerFactory, ILogger logger)
		{
			_serializerFactory = serializerFactory;
			_logger = logger;
			var serviceType = typeof(TService);
			var serviceMethods = serviceType.GetMethods();

			serviceType.ThrowIfNotInterfaceType();

			ServiceDescriptor = ServiceDescriptor.Of<TService>();
			ServicePort = serviceType.GetCustomAttribute<ServiceAttribute>()?.Port ?? NetworkConstants.DefaultPort;

			var (asyncMethods, syncMethods) = serviceMethods.Partition(
				methodInfo => typeof(Task).IsAssignableFrom(methodInfo.ReturnType));
			var (asyncOperationNames, syncOperationNames) = 
				ServiceDescriptor.OperationNameCodeDictionary.Keys
				.Partition(key => asyncMethods.Any(methodInfo => methodInfo.GetFullMethodName() == key));
				

			OperationCodesMap = syncOperationNames.ToDictionary(
					keySelector: key => ServiceDescriptor
						.OperationNameCodeDictionary[key],
					elementSelector: operationName => CreateExpressionForMethod(
						syncMethods.Single(methodInfo => methodInfo.GetFullMethodName() == operationName)));

			_logger.LogDebug(
				$@"Operations codes map construction for service {ServiceDescriptor.ServiceCode} 
				   completed for operation codes {
						OperationCodesMap.Keys.Aggregate(
							string.Empty,
							(aggregatedString, next) => $"{aggregatedString} \n {next}")
					}");

			AsyncOperationCodesMap = asyncOperationNames.ToDictionary(
				key => ServiceDescriptor.OperationNameCodeDictionary[key],
				operationName => CreateExpressionForAsyncMethod(
					asyncMethods.Single(methodInfo => methodInfo.GetFullMethodName() == operationName)));
		}

		private ServiceDescriptor ServiceDescriptor { get; }

		public int ServicePort { get; }

		private IDictionary<int, Func<TService, byte[][], ServiceCallResult>> OperationCodesMap { get; }
		
		private IDictionary<int, Func<TService, byte[][], AsyncOperationCallResult>> AsyncOperationCodesMap { get; }

		public ServiceCallResult Dispatch(TService serviceInstance, RpcMessage message)
		{
			if (!OperationCodesMap.TryGetValue(message.OperationCode, out var operation))
			{
				_logger.LogError(
					$"Method with operation code {message.OperationCode} was not found in service with code {message.ServiceCode}");
				return ServiceCallResult.CreateServiceCallResultWithException(
					ExceptionsSerializer.Instance.Serialize(
						new RpcDispatchingException(message.ServiceCode, message.OperationCode)));
			}

			try
			{
				return operation(serviceInstance, message.ArgumentsData);
			}
			catch (Exception exception)
			{
				return ServiceCallResult.CreateServiceCallResultWithException(
					ExceptionsSerializer.Instance.Serialize(exception));
			}
		}

		public async Task<ServiceCallResult> DispatchAsync(TService service, RpcMessage message)
		{
			if (!AsyncOperationCodesMap.TryGetValue(message.OperationCode, out var operation))
			{
				_logger.LogError(
					$"Method with operation code {message.OperationCode} was not found in service with code {message.ServiceCode}");
				return ServiceCallResult.CreateServiceCallResultWithException(
					ExceptionsSerializer.Instance.Serialize(
						new RpcDispatchingException(message.ServiceCode, message.OperationCode)));
			}

			try
			{
				var asyncOperationCallResult = operation(service, message.ArgumentsData);
				
				if (asyncOperationCallResult.IsVoid)
				{
					await asyncOperationCallResult.Task;
					return ServiceCallResult.GetVoidServiceCallResult();
				}

				await asyncOperationCallResult.Task.ConfigureAwait(false);
				return asyncOperationCallResult.GetResult(asyncOperationCallResult.Task);
			}
			catch (Exception exception)
			{
				return ServiceCallResult.CreateServiceCallResultWithException(
					ExceptionsSerializer.Instance.Serialize(exception));
			}
		}

		private Func<TService, byte[][], AsyncOperationCallResult> CreateExpressionForAsyncMethod(MethodInfo methodInfo)
		{
			try
			{
				var messageDataParameter = Expression.Parameter(typeof(byte[][]), "messageData");
				var serviceInstanceParameter = Expression.Parameter(typeof(TService), "serviceInstance");
				var serializerFactoryParameter = Expression.Constant(_serializerFactory);
				var parametersCalls = 
					CreateParametersCallExpression(methodInfo, serializerFactoryParameter, messageDataParameter);
				
				NewExpression createAsyncServiceCallResultExpression;
				var isVoidResult = methodInfo.ReturnType == typeof(Task);
				if (isVoidResult)
				{
					var asyncOperationCallResultConstructorInfo =
						FindAsyncOperationCallResultConstructor(new[] {typeof(Task)});

					createAsyncServiceCallResultExpression = Expression.New(
						asyncOperationCallResultConstructorInfo,
						Expression.Call(serviceInstanceParameter, methodInfo, parametersCalls));
				}
				else
				{
					var taskParameter = Expression.Parameter(typeof(Task), "task");
					var taskGenericParameterType = methodInfo.ReturnType.GenericTypeArguments.First();
					var getResultFromTaskExpression = Expression.Property(
						Expression.Convert(taskParameter, typeof(Task<>).MakeGenericType(taskGenericParameterType)),
						methodInfo.ReturnType,
						"Result");
					var serializeResultExpression = Expression.Call(
						type: typeof(ServiceCallResult),
						methodName: nameof(ServiceCallResult.CreateServiceCallResultWithReturnValue),
						typeArguments: null,
						arguments: Expression.Call(
							Expression.Call(
								serializerFactoryParameter,
								"CreateSerializer",
								new[] {taskGenericParameterType}),
							"Serialize",
							null,
							getResultFromTaskExpression));

					var asyncOperationCallResultConstructorInfo = FindAsyncOperationCallResultConstructor(
						new[] {typeof(Task), typeof(Func<Task, ServiceCallResult>)});

					createAsyncServiceCallResultExpression = Expression.New(
						asyncOperationCallResultConstructorInfo,
						Expression.Call(serviceInstanceParameter, methodInfo, parametersCalls),
						Expression.Lambda<Func<Task, ServiceCallResult>>(serializeResultExpression, taskParameter));
				}

				return Expression
					.Lambda<Func<TService, byte[][], AsyncOperationCallResult>>(
						createAsyncServiceCallResultExpression,
						serviceInstanceParameter,
						messageDataParameter).Compile();
			}
			catch (Exception ex)
			{
				throw new Exception("Service calls handlers construction failed", ex);
			}
		}

		private Func<TService, byte[][], ServiceCallResult> CreateExpressionForMethod(MethodInfo methodInfo)
		{
			try
			{
				var messageDataParameter = Expression.Parameter(typeof(byte[][]), "messageData");
				var serviceInstanceParameter = Expression.Parameter(typeof(TService), "serviceInstance");
				var serializerFactoryParameter = Expression.Constant(_serializerFactory);
				var parametersCalls = 
					CreateParametersCallExpression(methodInfo, serializerFactoryParameter, messageDataParameter);
				var serviceCall = Expression.Call(serviceInstanceParameter, methodInfo, parametersCalls);

				return Expression
					.Lambda<Func<TService, byte[][], ServiceCallResult>>(
						CreateServiceMethodCallExpression(methodInfo, serviceCall, serializerFactoryParameter),
						serviceInstanceParameter,
						messageDataParameter).Compile();
			}
			catch (Exception ex)
			{
				throw new Exception("Service calls handlers construction failed", ex);
			}
		}
		
		private static MethodCallExpression[] CreateParametersCallExpression(
			MethodInfo methodInfo, 
			Expression serializerFactoryParameter,
			Expression messageDataParameter)
		{
			var methodParameters = methodInfo.GetParameters();
			var parametersCalls = new MethodCallExpression[methodParameters.Length];
			for (var i = 0; i < parametersCalls.Length; i++)
			{
				parametersCalls[i] = Expression.Call(Expression.Call(
						serializerFactoryParameter,
						nameof(ISerializerFactory.CreateSerializer),
						new[] {methodParameters[i].ParameterType}),
					nameof(ISerializer<TService>.Deserialize),
					null,
					Expression.ArrayIndex(messageDataParameter, Expression.Constant(i)));
			}

			return parametersCalls;
		}
		
		private static ConstructorInfo FindAsyncOperationCallResultConstructor(Type[] argumentTypes)
		{
			var result = typeof(AsyncOperationCallResult).GetConstructor(argumentTypes);
			if (result == null)
			{
				throw new Exception("Target constructor for AsyncOperationCallResult was not found");
			}
			return result;
		}

		private static Expression CreateServiceMethodCallExpression(
			MethodInfo methodInfo, 
			Expression serviceCall, 
			Expression serializerFactoryParameter)
		{
			Expression resultExpression;
			if (methodInfo.ReturnType != typeof(void))
			{
				resultExpression = Expression.Call(
					type: typeof(ServiceCallResult),
					methodName: nameof(ServiceCallResult.CreateServiceCallResultWithReturnValue),
					typeArguments: null,
					arguments: Expression.Call(
						Expression.Call(
							serializerFactoryParameter,
							"CreateSerializer",
							new[] {methodInfo.ReturnType}),
						"Serialize",
						null,
						serviceCall));
			}
			else
			{
				resultExpression = Expression.Block(
					serviceCall,
					Expression.Call(typeof(ServiceCallResult),
						nameof(ServiceCallResult.GetVoidServiceCallResult), null, null));
			}

			return resultExpression;
		}

		private readonly ISerializerFactory _serializerFactory;
		private readonly ILogger _logger;
	}
}