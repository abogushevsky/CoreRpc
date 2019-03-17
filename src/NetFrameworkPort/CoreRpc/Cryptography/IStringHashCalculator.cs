namespace CoreRpc.Cryptography
{
	public interface IStringHashCalculator
	{
		long GetInt64Hash(string inputString);
	}
}