using System;
using System.Security.Cryptography;
using System.Text;

namespace Assembly.Helpers.Cryptography
{
	public class RsaCrypto
	{
		private static readonly RSACryptoServiceProvider MRsaProvider = new RSACryptoServiceProvider(2048);

		public static string EncryptData(string strData2Encrypt, string publickey)
		{
			string fullKey = publickey.Substring(31);
			MRsaProvider.FromXmlString(fullKey);
			byte[] byteData = Encoding.UTF32.GetBytes(strData2Encrypt);
			const int maxLength = 214;
			int dataLength = byteData.Length;
			int iterations = dataLength/maxLength;

			var sb = new StringBuilder();
			for (int i = 0; i <= iterations; i++)
			{
				var tempBytes = new byte[(dataLength - maxLength*i > maxLength) ? maxLength : dataLength - maxLength*i];
				Buffer.BlockCopy(byteData, maxLength*i, tempBytes, 0, tempBytes.Length);

				byte[] encbyteData = MRsaProvider.Encrypt(tempBytes, false);
				sb.Append(Convert.ToBase64String(encbyteData));
			}
			return sb.ToString();
		}


		public static string DecryptData(string strData2Decrypt, string privatekey)
		{
			string fullKey = privatekey.Substring(31);
			var rsa = new RSACryptoServiceProvider(2048);
			rsa.FromXmlString(fullKey);
			const int base64BlockSize = (256%3 != 0) ? ((256/3)*4) + 4 : (256/3)*4;
			int iterations = strData2Decrypt.Length/base64BlockSize;
			int l = 0;
			var fullbytes = new byte[0];
			for (int i = 0; i < iterations; i++)
			{
				byte[] encBytes = Convert.FromBase64String(strData2Decrypt.Substring(base64BlockSize*i, base64BlockSize));
				byte[] bytes = rsa.Decrypt(encBytes, false);
				Array.Resize(ref fullbytes, fullbytes.Length + bytes.Length);
				foreach (byte t in bytes)
				{
					fullbytes[l] = t;
					l++;
				}
			}

			return Encoding.UTF32.GetString(fullbytes);
		}
	}
}