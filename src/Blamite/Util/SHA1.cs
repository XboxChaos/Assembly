namespace Blamite.Util
{
	public static class SHA1
	{
		private static readonly System.Security.Cryptography.SHA1 _sha1 = System.Security.Cryptography.SHA1.Create();

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
