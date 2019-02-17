using System;
using System.Linq;

namespace CoreRpc.Networking.Rpc
{
	public class ServiceCallResult
	{
		public ServiceCallResult()
		{
			ReturnValue = Exception = Array.Empty<byte>();
		}
		
		public bool HasReturnValue => !HasException && ReturnValue.Any();

		public byte[] ReturnValue { get; private set; }

		public bool HasException => Exception.Any();
		
		public byte[] Exception { get; private set; }

		public static ServiceCallResult CreateServiceCallResultWithReturnValue(byte[] returnValue)
			=> new ServiceCallResult
			{
				ReturnValue = returnValue
			};

		public static ServiceCallResult CreateServiceCallResultWithException(byte[] exceptionData)
			=> new ServiceCallResult
			{
				Exception = exceptionData
			};

		public static ServiceCallResult GetVoidServiceCallResult() => _voidResult;

		private static readonly ServiceCallResult _voidResult = new ServiceCallResult();
	}
}
