using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization;
using CoreRpc.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CoreRpc.CodeGeneration
{
	internal static class CodeGenerationHelper
	{
		public static string CreateParametersListString(params MethodParameter[] methodParameters) =>
			methodParameters.Aggregate(
				seed: "(",
				func: (result, parameter) => $"{result} {parameter.Type.FullName} {parameter.Name},",
				resultSelector: result => $"{result.Remove(result.Length - 1)})");

		public static ClassDeclarationSyntax CreatePrivateReadonlyFieldFromParameter(
			this ClassDeclarationSyntax classDeclarationSyntax,
			string parameterName,
			Type fieldType) =>
			classDeclarationSyntax.AddMembers(SyntaxFactory
				.FieldDeclaration(
					SyntaxFactory
						.VariableDeclaration(SyntaxFactory.ParseTypeName(ParameterTypeToString(fieldType)))
						.AddVariables(SyntaxFactory.VariableDeclarator(NameToPrivateFieldName(parameterName))))
				.AddModifiers(
					SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
					SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

		public static string GetAggregatedParametersString(IEnumerable<ParameterInfo> parameters) =>
			parameters.Aggregate(string.Empty,
				(aggregated, parameterInfo) => !string.IsNullOrEmpty(aggregated)
					? $"{aggregated}, {ParameterTypeToString(parameterInfo.ParameterType)} {parameterInfo.Name}"
					: $"{ParameterTypeToString(parameterInfo.ParameterType)} {parameterInfo.Name}");

		public static Type[] GetAllUniqueTypesUsedInServiceInterfaceDeclaration(Type serviceInterfaceType)
		{
			var methodsInfo = serviceInterfaceType.GetMethods().ToArray();
			return methodsInfo
				.SelectMany(methodInfo => GetAllGenericTypes(methodInfo.ReturnType))
				.Union(methodsInfo.SelectMany(methodInfo =>
					methodInfo.GetParameters().SelectMany(
						parameterInfo => GetAllGenericTypes(parameterInfo.ParameterType))))
				.Union(serviceInterfaceType.AsArray())
				.ToArray();
		}

		private static Type[] GetAllGenericTypes(Type type) =>
			type.GetGenericArguments().Any() ? 
				type.GetGenericArguments().SelectMany(GetAllGenericTypes).Union(new [] {type}).ToArray() : 
				new [] {type};

		public static StatementSyntax CreateFieldAssigmentStatement(string parameterName) =>
			CreateAssignmentStatement(NameToPrivateFieldName(parameterName), parameterName);

		public static BlockSyntax GetMethodBodyBlock(MethodInfo methodInfo, ServiceDescriptor serviceDescriptor)
		{
			var isAsyncMethod = AsyncHelper.IsAsyncMethod(methodInfo);
			return SyntaxFactory.Block(
				CreateAssignmentStatement(
					$"var {RpcMessageInstanceVariableName}",
					$"new {nameof(RpcMessage)}()"),
				CreateAssignmentStatement(
					$"{RpcMessageInstanceVariableName}.{nameof(RpcMessage.ServiceCode)}",
					serviceDescriptor.ServiceCode.ToString()),
				CreateAssignmentStatement(
					$"{RpcMessageInstanceVariableName}.{nameof(RpcMessage.OperationCode)}",
					serviceDescriptor.GetOperationCodeByMethodInfo(methodInfo).ToString()),
				CreateAssignmentStatement(
					$"{RpcMessageInstanceVariableName}.{nameof(RpcMessage.IsAsyncOperation)}",
					isAsyncMethod.ToString().ToLower()),
				CreateAssignmentStatement(
					$"{RpcMessageInstanceVariableName}.{nameof(RpcMessage.ArgumentsData)}",
					$"{GetParametersSerializationCode(methodInfo.GetParameters().Select(parameterInfo => new MethodParameter(parameterInfo)).ToArray())}"),
				SyntaxFactory.ParseStatement(GetRemoteCallCode(methodInfo, isAsyncMethod)));
		}

		private static string GetParametersSerializationCode(MethodParameter[] parameters)
		{
			if (!parameters.Any())
			{
				return "Array.Empty<byte[]>()";
			}
			
			return parameters.Aggregate(
				seed: "new [] {",
				func: (result, parameter) =>
					$"{result} {GetSerializerCallString(parameter.Type, parameter.Name, true)},",
				resultSelector: result => $"{result.Remove(result.Length - 1)} }}");
		}

		private static string GetSerializerCallString(Type serializableType, string parameterName, bool doSerialize) =>
			$"{SerializerFactoryFieldName}.{nameof(ISerializerFactory.CreateSerializer)}<{ParameterTypeToString(serializableType)}>().{GetSerializerMethodName(doSerialize)}({parameterName})";

		private static string GetSerializerMethodName(bool doSerialize) =>
			doSerialize ? nameof(ISerializer<object>.Serialize) : nameof(ISerializer<object>.Deserialize);

		private static string NameToPrivateFieldName(string originalName) => 
			$"_{originalName.Substring(0, 1).ToLower()}{originalName.Substring(1)}";
		
		private static StatementSyntax CreateAssignmentStatement(string leftSide, string rightSide) =>
			SyntaxFactory.ParseStatement($"{leftSide} = {rightSide};");

		public static string ParameterTypeToString(Type parameterType)
		{
			if (parameterType == typeof(void))
			{
				return "void";
			}
			
			if (!parameterType.GenericTypeArguments.Any())
			{
				return parameterType.Name;
			}

			if (parameterType.Name.Contains($"{nameof(ValueTuple)}"))
			{
				return parameterType.GenericTypeArguments.Aggregate(
					seed: "(",
					func: (result, parameter) => $"{result} {parameter.Name},",
					resultSelector: result => $"{result.Remove(result.Length - 1)}) ");
			}
			
			return
				$@"{parameterType.Name.Substring(
					0, 
					parameterType.Name.IndexOf('`'))}<{parameterType.GetGenericArguments()
					.Aggregate(
						string.Empty, 
						(result, parameter) => $"{result} {ParameterTypeToString(parameter)},",
						result => result.Remove(result.Length - 1))}>";
		}

		private static string GetRemoteCallCode(MethodInfo methodInfo, bool isAsyncMethod)
		{
			var messageSerializationCode = GetSerializerCallString(
				typeof(RpcMessage), 
				RpcMessageInstanceVariableName, 
				true);
			if (methodInfo.ReturnType == typeof(void))
			{
				return $"{TcpClientFieldName}.{nameof(RpcTcpClientBase.Send)}({messageSerializationCode});";
			}

			if (AsyncHelper.IsVoidAsyncMethod(methodInfo))
			{
				return $"await {TcpClientFieldName}.{nameof(RpcTcpClientBase.SendAsync)}({messageSerializationCode});";
			}

			var sendAndRecieveCallCode = isAsyncMethod ? 
				$"await {TcpClientFieldName}.{nameof(RpcTcpClientBase.SendAndReceiveAsync)}({messageSerializationCode})" 
				: $"{TcpClientFieldName}.{nameof(RpcTcpClientBase.SendAndReceive)}({messageSerializationCode})";
			var typeToDeserialize = isAsyncMethod
				? methodInfo.ReturnType.GetGenericArguments().First()
				: methodInfo.ReturnType;
			
			return $"return {GetSerializerCallString(typeToDeserialize, sendAndRecieveCallCode, false)};";
		}

		private const string RpcMessageInstanceVariableName = "rpcMessage";
		private const string SerializerFactoryFieldName = "_serializerFactory";
		private const string TcpClientFieldName = "_tcpClient";
	}
}