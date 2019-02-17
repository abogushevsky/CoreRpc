using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRpc.Networking
{
	internal static class NetworkHelper
	{
		public static byte[] WithEndOfMessage(this byte[] originalMessageBytes) =>
			originalMessageBytes.Concat(NetworkConstants.EndOfMessageBytes).ToArray();

		public static void WriteEndOfSessionMessage(this Stream stream) => 
			WriteMessage(stream, NetworkConstants.EndOfSessionMessageBytes);

		public static void WriteMessage(this Stream stream, byte[] message)
		{
			var messageWithEndOfMessage = message.WithEndOfMessage();
			stream.Write(messageWithEndOfMessage, 0, messageWithEndOfMessage.Length);
		}

		public static async Task WriteMessageAsync(this Stream stream, byte[] message)
		{
			var messageWithEndOfMessage = message.WithEndOfMessage();
			await stream.WriteAsync(messageWithEndOfMessage, 0, messageWithEndOfMessage.Length);
		}

		public static byte[] ReadMessage(this Stream stream)
		{
			var receivedData = new List<byte>();
			// ReSharper disable once RedundantAssignment actually it's not redundant
			var bytesRead = -1;
			do
			{
				var dataChunk = new byte[NetworkConstants.DataChunkSize];
				bytesRead = stream.Read(dataChunk, 0, dataChunk.Length);
				receivedData.AddRange(dataChunk.Take(bytesRead).ToArray());

				if (receivedData.ContainsEndOfMessage())
				{
					break;
				}
			} while (bytesRead != 0);

			return receivedData.ToArray();
		}
		
		public static async Task<byte[]> ReadMessageAsync(this Stream stream)
		{
			var receivedData = new List<byte>();
			// ReSharper disable once RedundantAssignment actually it's not redundant
			var bytesRead = -1;
			do
			{
				var dataChunk = new byte[NetworkConstants.DataChunkSize];
				bytesRead = await stream.ReadAsync(dataChunk, 0, dataChunk.Length);
				receivedData.AddRange(dataChunk.Take(bytesRead).ToArray());

				if (receivedData.ContainsEndOfMessage())
				{
					break;
				}
			} while (bytesRead != 0);

			return receivedData.ToArray();
		}

		private static bool ContainsEndOfMessage(this List<byte> messageBytes) =>
			messageBytes
				.Skip(messageBytes.Count - NetworkConstants.EndOfMessageBytes.Length)
				.Take(NetworkConstants.EndOfMessageBytes.Length)
				.SequenceEqual(NetworkConstants.EndOfMessageBytes);
	}
}