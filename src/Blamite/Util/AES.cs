using System.Security.Cryptography;

namespace Blamite.Util
{
	public static class AES
	{
		/// <summary>
		///     The size of a single AES block.
		/// </summary>
		public const int BlockSize = 0x10;

		/// <summary>
		///     Aligns a size value to be a multiple of the AES block size (0x10).
		/// </summary>
		/// <param name="size">The size to align.</param>
		/// <returns>The aligned size.</returns>
		public static int AlignSize(int size)
		{
			return (size + BlockSize - 1) & ~(BlockSize - 1);
		}

		public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
		{
			var crypto = new AesCryptoServiceProvider();
			crypto.Key = key;
			crypto.IV = iv;
			crypto.Padding = PaddingMode.None;
			ICryptoTransform transformer = crypto.CreateDecryptor();
			return transformer.TransformFinalBlock(data, 0, data.Length);
		}

		public static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
		{
			var crypto = new AesCryptoServiceProvider();
			crypto.Key = key;
			crypto.IV = iv;
			crypto.Padding = PaddingMode.None;
			ICryptoTransform transformer = crypto.CreateDecryptor();
			return transformer.TransformFinalBlock(data, offset, length);
		}

		public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
		{
			var crypto = new AesCryptoServiceProvider();
			crypto.Key = key;
			crypto.IV = iv;
			crypto.Padding = PaddingMode.None;
			ICryptoTransform transformer = crypto.CreateEncryptor();
			return transformer.TransformFinalBlock(data, 0, data.Length);
		}

		public static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
		{
			var crypto = new AesCryptoServiceProvider();
			crypto.Key = key;
			crypto.IV = iv;
			crypto.Padding = PaddingMode.None;
			ICryptoTransform transformer = crypto.CreateEncryptor();
			return transformer.TransformFinalBlock(data, offset, length);
		}
	}
}