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
			
			OperationCodesMap = ServiceDescriptor.OperationNameCodeDictionary.Keys.ToDictionary(
				keySelector: key => ServiceDescriptor
					.OperationNameCodeDictionary[key],
				elementSelector: operationName => CreateExpressionForMethod(
					serviceMethods.Single(methodInfo => methodInfo.GetFullMethodName() == operationName)));

			_logger.LogDebug(
				$@"Operations codes map construction for service {ServiceDescriptor.ServiceCode} 
				   completed for operation codes {
						OperationCodesMap.Keys.Aggregate(
							string.Empty,
							(aggregatedString, next) => $"{aggregatedString} \n {next}")
					}");
		}

		private ServiceDescriptor ServiceDescriptor { get; }

		public int ServicePort { get; }

		private IDictionary<int, Func<TService, byte[][], ServiceCallResult>> OperationCodesMap { get; }

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

		// TODO: Maybe useless. Remove this and IsAsync property from RpcMessage
		public async Task<ServiceCallResult> DispatchAsync(TService service, RpcMessage message)
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
				throw new NotImplementedException();
			}
			catch (Exception exception)
			{
				return ServiceCallResult.CreateServiceCallResultWithException(
					ExceptionsSerializer.Instance.Serialize(exception));
			}
		}

		private Func<TService, byte[][], ServiceCallResult> CreateExpressionForMethod(MethodInfo methodInfo)
		{
			try
			{
				var serializerFactoryParameter = Expression.Constant(_serializerFactory);
				var serviceInstanceParameter = Expression.Parameter(typeof(TService), "serviceInstance");
				var messageDataParameter = Expression.Parameter(typeof(byte[][]), "messageData");

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

				var serviceCall = Expression.Call(serviceInstanceParameter, methodInfo, parametersCalls);

				var resultExpression = AsyncHelper.IsAsyncMethod(methodInfo)
					? CreateAsyncServiceCallExpression(methodInfo, serviceCall, serializerFactoryParameter)
					: CreateServiceMethodCallExpression(methodInfo, serviceCall, serializerFactoryParameter);

				return Expression
					.Lambda<Func<TService, byte[][], ServiceCallResult>>(
						resultExpression,
						serviceInstanceParameter,
						messageDataParameter).Compile();
			}
			catch (Exception ex)
			{
				throw new Exception("Service calls handlers construction failed", ex);
			}
		}

		private Expression CreateServiceMethodCallExpression(
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

		/*
		 * The idea is to create async wrapper-function that will do the following:
		 * 1) use pre-compiled lambda for parameters deserialization and service method call,
		 * /// 2) use precompiled lambda for service implementation method call with async keyword
		 * 3) use pre-compiled lambda to serialize result
		 * Will see if all of this steps are possible.
		 *
		 * TODO: Google for "How to Expression.Call async
		 */
		private Expression CreateAsyncServiceCallExpression(
			MethodInfo methodInfo, 
			Expression serviceCall, 
			Expression serializerFactoryParameter)
		{
			Expression resultExpression;
			
			if (AsyncHelper.IsVoidAsyncMethod(methodInfo))
			{
				throw new NotImplementedException();			
			}
			else
			{
				throw new NotImplementedException();
			}

			return resultExpression;
		}
		
		private async Task<ServiceCallResult> InvokeAsyncMethodCall<TResult>(
			Func<Task<TResult>> serviceMethodCall,
			Func<ServiceCallResult> resultCreationLambda)
		{
			var result = await serviceMethodCall();
			var serializedResult = _serializerFactory.CreateSerializer<TResult>().Serialize(result);
			return ServiceCallResult.CreateServiceCallResultWithReturnValue(serializedResult);
		}

		private readonly ISerializerFactory _serializerFactory;
		private readonly ILogger _logger;
	}
}
