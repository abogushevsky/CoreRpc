using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreRpc.Networking.Rpc.ServiceAnnotations;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.Rpc
{
    public class ServiceDescriptor
    {
	    private ServiceDescriptor(Type serviceType)
        {
	        _serviceType = serviceType;
	        ServiceName = serviceType.Name;
            ServiceCode = serviceType.FullName.GetHashCode();
	        ServicePort = serviceType.GetCustomAttribute<ServiceAttribute>()?.Port ?? NetworkConstants.DefaultPort;

			OperationNameCodeDictionary = serviceType.GetMethods().ToDictionary(
                keySelector: (_, methodInfo) => methodInfo.GetFullMethodName(),
                valueSelector: (currentIndex, _) => currentIndex);
            OperationCodes = serviceType.GetMethods().Select(methodInfo => methodInfo.Name.GetHashCode()).ToArray();
        }

	    public int ServicePort { get; }

	    public string ServiceName { get; }

        public int ServiceCode { get;  }

        public int[] OperationCodes { get; }
        
        public Dictionary<string, int> OperationNameCodeDictionary { get; }

	    public int GetOperationCodeByMethodInfo(MethodInfo methodInfo)
	    {
		    if (methodInfo.DeclaringType != _serviceType)
		    {
			    throw new ArgumentException($"Method {methodInfo.Name} should be declared ins service type {_serviceType.Name}");
		    }

		    if (!OperationNameCodeDictionary.TryGetValue(methodInfo.GetFullMethodName(), out var result))
		    {
			    throw new ArgumentException($"Operation code not found for method {methodInfo.Name} of service {_serviceType.Name}");
		    }

		    return result;
	    }

	    public static ServiceDescriptor Of<TService>()
        {
            var serviceType = typeof(TService);

            if (!serviceType.IsInterface)
            {
                throw new Exception("Service type should be an interface");
            }
            
            return new ServiceDescriptor(typeof(TService));
        }

	    private readonly Type _serviceType;
	}
}