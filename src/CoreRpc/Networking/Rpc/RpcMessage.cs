namespace Common.Infrastructure.Networking.Rpc
{
	public class RpcMessage
	{
		public int ServiceCode { get; set; }

		public int OperationCode { get; set; }

		public byte[][] ArgumentsData { get; set; }
	}
}