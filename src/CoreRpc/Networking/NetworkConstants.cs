using System.Text;

namespace CoreRpc.Networking
{
	internal static class NetworkConstants
	{
		public const int DefaultPort = 57001;
		
		public const int DataChunkSize = 8 * 1024;

		public const string EndOfMessage = "<EOM>";

		public const string EndOfSession = "<EndOfSession>";
		
		public static readonly byte[] EndOfMessageBytes = Encoding.UTF8.GetBytes(NetworkConstants.EndOfMessage);

		public static readonly byte[] EndOfSessionMessageBytes = Encoding.UTF8.GetBytes(NetworkConstants.EndOfSession);
	}
}