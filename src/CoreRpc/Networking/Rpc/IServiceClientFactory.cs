using System.Net.Security;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
    public interface IServiceClientFactory
    {
        ServiceClient<TService> CreateServiceClient<TService>(string hostName) where TService : class;

        ServiceClient<TService> CreateServiceClient<TService>(
            string hostName,
            ISerializerFactory serializerFactory,
            ClientParameters parameters = null) where TService : class;

        ServiceClient<TService> CreateSecuredServiceClient<TService>(
            string hostName,
            ISerializerFactory serializerFactory,
            RemoteCertificateValidationCallback serverCertificateValidationCallback,
            ClientParameters parameters = null) where TService : class;

        ServiceClient<TService> CreateSecuredServiceClient<TService>(
            string hostName,
            ISerializerFactory serializerFactory,
            RemoteCertificateValidationCallback serverCertificateValidationCallback,
            LocalCertificateSelectionCallback clientCertificateSelectionCallback,
            ClientParameters parameters = null) where TService : class;
    }
}