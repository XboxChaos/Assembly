using System;
using System.Text;

namespace Blamite.Util
{
	public static class FunctionHelpers
	{
		public static string BytesToString(byte[] data)
		{
			return RemoveNulls(Encoding.ASCII.GetString(data));
		}

		public static byte[] StringToBytes(string data)
		{
			return Encoding.ASCII.GetBytes(data);
		}

		public static string RemoveNulls(string str)
		{
			return str.Replace("\0", "");
		}

		public static string BytesToHexString(byte[] array)
		{
			var builder = new StringBuilder(array.Length*2);
			string chars = "0123456789ABCDEF";
			foreach (byte b in array)
			{
				builder.Append(chars[b >> 4]);
				builder.Append(chars[b & 0xF]);
			}
			return builder.ToString();
		}

		public static string BytesToHexLines(byte[] array, int newlineInterval)
		{
			var builder = new StringBuilder(array.Length*2);
			string chars = "0123456789ABCDEF";
			for (int i = 0; i < array.Length; i++)
			{
				if (i > 0 && i < array.Length - 1 && i%newlineInterval == 0)
					builder.AppendLine();

				builder.Append(chars[array[i] >> 4]);
				builder.Append(chars[array[i] & 0xF]);
			}
			return builder.ToString();
		}

		public static byte[] HexStringToBytes(string hex)
		{
			// Strip whitespace
			hex = hex.Replace(" ", "");
			hex = hex.Replace("\n", "");
			hex = hex.Replace("\r", "");
			hex = hex.Replace("\t", "");

			if (hex.Length%2 != 0)
				throw new FormatException("Hex string must be of even length");

			var result = new byte[hex.Length/2];
			for (int i = 0; i < hex.Length; i++)
			{
				int value;
				char ch = hex[i];
				if (ch >= '0' && ch <= '9')
					value = ch - '0';
				else if (ch >= 'A' && ch <= 'F')
					value = 0xA + ch - 'A';
				else if (ch >= 'a' && ch <= 'f')
					value = 0xA + ch - 'a';
				else
					throw new FormatException("Not a valid hex string");

				result[i/2] |= (byte) (value << (4*(1 - i%2)));
			}

			return result;
		}

		public static int FindBytes(byte[] src, byte[] find)
		{
			int index = -1;
			int matchIndex = 0;
			// handle the complete source array
			for (int i = 0; i < src.Length; i++)
			{
				if (src[i] == find[matchIndex])
				{
					if (matchIndex == (find.Length - 1))
					{
						index = i - matchIndex;
						break;
					}
					matchIndex++;
				}
				else
				{
					matchIndex = 0;
				}
			}
			return index;
		}

		public static byte[] ReplaceBytes(byte[] src, byte[] search, byte[] repl)
		{
			byte[] dst = null;
			int index = FindBytes(src, search);
			if (index >= 0)
			{
				dst = new byte[src.Length - search.Length + repl.Length];
				// before found array
				Buffer.BlockCopy(src, 0, dst, 0, index);
				// repl copy
				Buffer.BlockCopy(repl, 0, dst, index, repl.Length);
				// rest of src array
				Buffer.BlockCopy(
					src,
					index + search.Length,
					dst,
					index + repl.Length,
					src.Length - (index + search.Length));
			}
			return dst;
		}
	}
}