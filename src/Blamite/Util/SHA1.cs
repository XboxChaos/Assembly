using System.Security.Cryptography;

namespace Blamite.Util
{
	public static class SHA1
	{
		private static readonly SHA1CryptoServiceProvider _sha1 = new SHA1CryptoServiceProvider();

		public static byte[] Transform(byte[] data)
		{
			return _sha1.ComputeHash(data);
		}

		public static byte[] Transform(byte[] data, int offset, int length)
		{
			return _sha1.ComputeHash(data, offset, length);
		}
	}
}