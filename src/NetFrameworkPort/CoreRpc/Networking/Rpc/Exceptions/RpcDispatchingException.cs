using System;

namespace CoreRpc.Networking.Rpc.Exceptions
{
	public class RpcDispatchingException : Exception
	{
		public RpcDispatchingException(int serviceCode, int operationCode) : base(
			$"Method with operation code {operationCode} not found in service with code {serviceCode}")
		{
			
		}
	}
}