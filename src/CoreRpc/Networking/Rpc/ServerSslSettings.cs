using System;
using System.Security.Cryptography.X509Certificates;

namespace CoreRpc.Networking.Rpc
{
	public class ServerSslSettings
	{
		public ServerSslSettings(X509Certificate2 certificate, bool clientCertificateRequired, bool checkCertificateRevocation)
		{
			Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
			ClientCertificateRequired = clientCertificateRequired;
			CheckCertificateRevocation = checkCertificateRevocation;
		}
		
		public X509Certificate2 Certificate { get; }
		
		public bool ClientCertificateRequired { get; }
		
		public bool CheckCertificateRevocation { get; }
	}
}