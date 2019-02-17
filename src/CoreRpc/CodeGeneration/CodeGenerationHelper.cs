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
				.Select(methodInfo => methodInfo.ReturnType)
				.Union(methodsInfo.SelectMany(methodInfo =>
					methodInfo.GetParameters().Select(parameterInfo => parameterInfo.ParameterType)))
				.Union(serviceInterfaceType.AsArray())
				.ToArray();
		}

		public static StatementSyntax CreateFieldAssigmentStatement(string parameterName) =>
			CreateAssignmentStatement(NameToPrivateFieldName(parameterName), parameterName);

		public static BlockSyntax GetMethodBodyBlock(MethodInfo methodInfo, ServiceDescriptor serviceDescriptor) =>
			SyntaxFactory.Block(
				CreateAssignmentStatement($"var {rpcMessageInstanceVariableName}", $"new {nameof(RpcMessage)}()"),
				CreateAssignmentStatement(
					$"{rpcMessageInstanceVariableName}.{nameof(RpcMessage.ServiceCode)}",
					serviceDescriptor.ServiceCode.ToString()),
				CreateAssignmentStatement(
					$"{rpcMessageInstanceVariableName}.{nameof(RpcMessage.OperationCode)}",
					serviceDescriptor.GetOperationCodeByMethodInfo(methodInfo).ToString()),
				CreateAssignmentStatement(
					$"{rpcMessageInstanceVariableName}.{nameof(RpcMessage.ArgumentsData)}",
					$"{GetParametersSerializationCode(methodInfo.GetParameters().Select(parameterInfo => new MethodParameter(parameterInfo)).ToArray())}"),
				SyntaxFactory.ParseStatement(GetRemoteCallCode(methodInfo)));

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
			$"{serializerFactoryFieldName}.{nameof(ISerializerFactory.CreateSerializer)}<{ParameterTypeToString(serializableType)}>().{GetSerializerMethodName(doSerialize)}({parameterName})";

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

			if (parameterType.FullName.Contains($"{typeof(ValueTuple).Namespace}.{typeof(ValueTuple).Name}"))
			{
				return parameterType.GenericTypeArguments.Aggregate(
					seed: "(",
					func: (result, parameter) => $"{result} {parameter.Name},",
					resultSelector: result => $"{result.Remove(result.Length - 1)}) ");
			}

			return parameterType.GenericTypeArguments.Aggregate(
				seed: $"{parameterType.Name.Remove(parameterType.Name.LastIndexOf('`'))}<",
				func: (result, parameter) => $"{result} {parameter.Name},",
				resultSelector: result => $"{result.Remove(result.Length - 1)}> ");
		}

		private static string GetRemoteCallCode(MethodInfo methodInfo)
		{
			var messageSerializationCode = GetSerializerCallString(typeof(RpcMessage), rpcMessageInstanceVariableName, true);
			if (methodInfo.ReturnType == typeof(void))
			{
				return $"{tcpClientFieldName}.{nameof(RpcTcpClientBase.Send)}({messageSerializationCode});";
			}

			var sendAndRecieveCallCode = $"{tcpClientFieldName}.{nameof(RpcTcpClientBase.SendAndReceive)}({messageSerializationCode})";
			return $"return {GetSerializerCallString(methodInfo.ReturnType, sendAndRecieveCallCode, false)};";
		}

		private const string rpcMessageInstanceVariableName = "rpcMessage";
		private const string serializerFactoryFieldName = "_serializerFactory";
		private const string tcpClientFieldName = "_tcpClient";
	}
}