using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using Common.Infrastructure.Networking.Rpc;
using CoreRpc.CodeGeneration;
using CoreRpc.Logging;
using CoreRpc.Serialization;
using CoreRpc.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CoreRpc.Networking.Rpc
{
	[SuppressMessage("ReSharper", "CoVariantArrayConversion")]
	public static class ServiceClientFactory
	{
		public static TService CreateServiceClient<TService>(
			string hostName, 
			ILogger logger,
			ISerializerFactory serializerFactory) where TService : class
		{
			var serviceDescriptor = ServiceDescriptor.Of<TService>();
			var tcpClient = new UnprotectedRpcTcpClient(hostName, serviceDescriptor.ServicePort, serializerFactory);

			return CreateServiceClientInstance<TService>(
				       hostName,
				       logger,
				       serializerFactory,
				       serviceDescriptor,
				       tcpClient);
		}
		
		public static TService CreateSecuredServiceClient<TService>(
			string hostName, 
			ILogger logger,
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback) where TService : class
		{
			var serviceDescriptor = ServiceDescriptor.Of<TService>();
			var tcpClient = new SslRpcTcpClient(
				hostName, 
				serviceDescriptor.ServicePort, 
				serializerFactory, 
				serverCertificateValidationCallback);

			return CreateServiceClientInstance<TService>(
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient);
		}
		
		public static TService CreateSecuredServiceClient<TService>(
			string hostName, 
			ILogger logger,
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			LocalCertificateSelectionCallback clientSertificateSelectionCallback) where TService : class
		{
			var serviceDescriptor = ServiceDescriptor.Of<TService>();
			var tcpClient = new SslRpcTcpClient(
				hostName, 
				serviceDescriptor.ServicePort, 
				serializerFactory, 
				serverCertificateValidationCallback,
				clientSertificateSelectionCallback);

			return CreateServiceClientInstance<TService>(
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient);
		}

		private static TService CreateServiceClientInstance<TService>(
			string hostName,
			ILogger logger,
			ISerializerFactory serializerFactory,
			ServiceDescriptor serviceDescriptor,
			RpcTcpClientBase tcpClient) where TService : class
		{
			
			var serviceType = typeof(TService);
			serviceType.ThrowIfNotInterfaceType();

			var allUniqueTypesUsingInServiceInterfaceDeclaration =
				CodeGenerationHelper.GetAllUniqueTypesUsedInServiceInterfaceDeclaration(serviceType).Union(
					new[]
					{
						typeof(ServiceDescriptor),
						typeof(RpcTcpClientBase)
					}).ToArray();

			var className = $"Generated_{serviceType.Name}Client";

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System"))
					.AsArray()
					.Union(allUniqueTypesUsingInServiceInterfaceDeclaration
						.Select(type => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(type.Namespace))))
					.Union(new[]
					{
						SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(typeof(ILogger).Namespace)),
						SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(typeof(ISerializerFactory).Namespace))
					})
					.Distinct()
					.ToArray())
				.AddMembers(
					SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("TestNamespace"))
						.AddMembers(
							SyntaxFactory.ClassDeclaration(className)
								.AddModifiers(SyntaxFactory.ParseToken("public"), SyntaxFactory.ParseToken("sealed"))
								.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(serviceType.Name)))
								.CreatePrivateReadonlyFieldFromParameter(nameof(hostName), typeof(string))
								.CreatePrivateReadonlyFieldFromParameter(nameof(logger), typeof(ILogger))
								.CreatePrivateReadonlyFieldFromParameter(nameof(serializerFactory), typeof(ISerializerFactory))
								.CreatePrivateReadonlyFieldFromParameter(nameof(serviceDescriptor), typeof(ServiceDescriptor))
								.CreatePrivateReadonlyFieldFromParameter(nameof(tcpClient), typeof(RpcTcpClientBase))
								.AddMembers(SyntaxFactory.ConstructorDeclaration(className)
									.AddModifiers(SyntaxFactory.ParseToken("public"))
									.WithParameterList(
										SyntaxFactory.ParseParameterList(
											CodeGenerationHelper.CreateParametersListString(
												new MethodParameter(typeof(string), nameof(hostName)),
												new MethodParameter(typeof(ILogger), nameof(logger)),
												new MethodParameter(typeof(ISerializerFactory), nameof(serializerFactory)),
												new MethodParameter(typeof(ServiceDescriptor), nameof(serviceDescriptor)),
												new MethodParameter(typeof(RpcTcpClientBase), nameof(tcpClient)))))
									.WithBody(SyntaxFactory.Block(
										CodeGenerationHelper.CreateFieldAssigmentStatement(nameof(hostName)),
										CodeGenerationHelper.CreateFieldAssigmentStatement(nameof(logger)),
										CodeGenerationHelper.CreateFieldAssigmentStatement(nameof(serializerFactory)),
										CodeGenerationHelper.CreateFieldAssigmentStatement(nameof(serviceDescriptor)),
										CodeGenerationHelper.CreateFieldAssigmentStatement(nameof(tcpClient)))))
								.AddMembers(
									serviceType.GetMethods()
										.Select(methodInfo =>
											SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(CodeGenerationHelper.ParameterTypeToString(methodInfo.ReturnType)), methodInfo.Name)
												.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
												.WithParameterList(
													SyntaxFactory.ParseParameterList($"({CodeGenerationHelper.GetAggregatedParametersString(methodInfo.GetParameters())})"))
												.WithBody(CodeGenerationHelper.GetMethodBodyBlock(methodInfo, serviceDescriptor)))
										.ToArray())));

			var references = allUniqueTypesUsingInServiceInterfaceDeclaration
				.Select(type => type.Assembly.Location)
				.Union(
					Assembly
						.GetExecutingAssembly()
						.GetReferencedAssemblies()
						.Select(assemblyName => Assembly
							.Load(assemblyName)
							.Location).Union(new[]
						{
							Assembly.GetExecutingAssembly().Location,
							typeof(object).Assembly.Location,
							typeof(Console).Assembly.Location
						}))
				.Distinct()
				.Select(assemblyLocation => MetadataReference.CreateFromFile(assemblyLocation))
				.ToArray();

			var options = new CSharpCompilationOptions(
				outputKind: OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				platform: Platform.X64); // TODO: determine current platform

			var generatedCode = compilationUnit.NormalizeWhitespace().ToFullString();
			logger.LogDebug(generatedCode);
			var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
			var compilation = CSharpCompilation.Create("InMemoryAssembly")
				.AddSyntaxTrees(syntaxTree)
				.AddReferences(references)
				.WithOptions(options);

			var stream = new MemoryStream();
			var emitResult = compilation.Emit(stream);

			if (!emitResult.Success)
			{
				return null;
			}

			stream.Seek(0, SeekOrigin.Begin);
			var generatedAssembly = Assembly.Load(stream.ToArray());
			var generatedClass = generatedAssembly.DefinedTypes.Single();
			return Activator.CreateInstance(
				generatedClass,
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient) as TService;
		}
	}
}