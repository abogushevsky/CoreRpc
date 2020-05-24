namespace CoreRpc.Cryptography
{
	internal interface IStringHashCalculator
	{
		long GetInt64Hash(string inputString);
	}
}