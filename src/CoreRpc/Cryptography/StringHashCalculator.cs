using System;
using System.Security.Cryptography;
using System.Text;

namespace CoreRpc.Cryptography
{
	// TODO: add perf tests
	internal class StringHashCalculator : IStringHashCalculator, IDisposable
	{
		public StringHashCalculator(HashAlgorithm hashAlgorithm)
		{
			_hashAlgorithm = hashAlgorithm;
			_hashAlgorithm.Initialize();
		}

		public long GetInt64Hash(string inputString)
		{
			var inputUtf8Bytes = Encoding.UTF8.GetBytes(inputString);
			var hashText = _hashAlgorithm.ComputeHash(inputUtf8Bytes);

			//32Byte hashText separate
			//hashCodeStart = 0~7  8Byte
			//hashCodeMedium = 8~23  8Byte
			//hashCodeEnd = 24~31  8Byte
			//and Fold
			var hashCodeStart = BitConverter.ToInt64(hashText, 0);
			var hashCodeMedium = BitConverter.ToInt64(hashText, 8);
			var hashCodeEnd = BitConverter.ToInt64(hashText, 24);
			return hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
		}

		public void Dispose() => _hashAlgorithm.Dispose();

		private readonly HashAlgorithm _hashAlgorithm;
	}
}