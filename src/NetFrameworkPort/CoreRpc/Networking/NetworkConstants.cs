using System.Text;

namespace CoreRpc.Networking
{
	internal static class NetworkConstants
	{
		public const int DefaultPort = 57001;
		
		public const int DataChunkSize = 8 * 1024;		
		
		public static readonly byte[] EndOfMessageBytes = Encoding.UTF8.GetBytes(EndOfMessage);

		public static readonly byte[] EndOfSessionMessageBytes = Encoding.UTF8.GetBytes(EndOfSession);
		
		private const string EndOfMessage = "<EOM>";

		private const string EndOfSession = "<EndOfSession>";
	}
}