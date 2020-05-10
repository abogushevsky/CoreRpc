using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Text;
using CoreRpc.CodeGeneration;
using CoreRpc.Logging;
using CoreRpc.Serialization;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CoreRpc.Networking.Rpc
{
	[SuppressMessage("ReSharper", "CoVariantArrayConversion")]
	public static class ServiceClientFactory
	{
		public static ServiceClient<TService> CreateServiceClient<TService>(
			string hostName,
			ILogger logger) where TService : class
		{
			return CreateServiceClient<TService>(hostName, logger, new MessagePackSerializerFactory());
		}
		
		public static ServiceClient<TService> CreateServiceClient<TService>(
			string hostName, 
			ILogger logger,
			ISerializerFactory serializerFactory) where TService : class
		{
			var serviceDescriptor = ServiceDescriptor.Of<TService>();
			var tcpClient = new UnprotectedRpcTcpClient(
				hostName, 
				serviceDescriptor.ServicePort, 
				serializerFactory, 
				logger);

			return CreateServiceClientInstance<TService>(
				       hostName,
				       logger,
				       serializerFactory,
				       serviceDescriptor,
				       tcpClient);
		}
		
		public static ServiceClient<TService> CreateSecuredServiceClient<TService>(
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
				serverCertificateValidationCallback,
				logger);

			return CreateServiceClientInstance<TService>(
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient);
		}
		
		public static ServiceClient<TService> CreateSecuredServiceClient<TService>(
			string hostName, 
			ILogger logger,
			ISerializerFactory serializerFactory,			
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			LocalCertificateSelectionCallback clientCertificateSelectionCallback) where TService : class
		{
			var serviceDescriptor = ServiceDescriptor.Of<TService>();
			var tcpClient = new SslRpcTcpClient(
				hostName, 
				serviceDescriptor.ServicePort, 
				serializerFactory,
				serverCertificateValidationCallback,
				clientCertificateSelectionCallback,
				logger);

			return CreateServiceClientInstance<TService>(
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient);
		}

		private static ServiceClient<TService> CreateServiceClientInstance<TService>(
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
					SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(serviceType.Namespace))
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
												.AddModifiers(GetModifiers(methodInfo))
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
				var compilerOutputMessageBuilder = new StringBuilder();
				compilerOutputMessageBuilder.AppendLine(
					$"There are following problems found during compillation client proxy for {typeof(TService).FullName}");
				emitResult.Diagnostics.ForEach(
					item => compilerOutputMessageBuilder.AppendLine($"{item.Severity}: {item.GetMessage()}"));
				logger.LogDebug(compilerOutputMessageBuilder.ToString());
				return null;
			}

			stream.Seek(0, SeekOrigin.Begin);
			var generatedAssembly = Assembly.Load(stream.ToArray());
			var generatedClass = generatedAssembly.DefinedTypes.Single(type => type.Name == className);
			var serviceInstance = Activator.CreateInstance(
				generatedClass,
				hostName,
				logger,
				serializerFactory,
				serviceDescriptor,
				tcpClient) as TService;
			
			return new ServiceClient<TService>(tcpClient, serviceInstance);
		}

		private static SyntaxToken[] GetModifiers(MethodInfo methodInfo) => 
			AsyncHelper.IsAsyncMethod(methodInfo) ? 
				new [] {SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword)} : 
				new [] {SyntaxFactory.Token(SyntaxKind.PublicKeyword)};
	}
}