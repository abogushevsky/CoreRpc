using System.Text;

namespace CoreRpc.Networking
{
	internal static class NetworkConstants
	{
		public const int DefaultPort = 57001;
		
		public const int DataChunkSize = 8 * 1024;

		public const string EndOfMessage = "<EOM>";
		
		public static readonly byte[] EndOfMessageBytes = Encoding.UTF8.GetBytes(NetworkConstants.EndOfMessage);
	}
}