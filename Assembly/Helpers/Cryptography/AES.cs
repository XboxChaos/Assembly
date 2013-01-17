using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Assembly.Helpers.Cryptography
{
    public class AesCrypto
    {
	    private const string Salt = "asm__";
	    private const string HashAlgo = "SHA1";
		private const int PassIter = 2;
		private const string InitVector = "OFRna73m*aze01xY";
		private const int KeySize = 256;

        public static string EncryptData(string strData, string key)
        {
            if (string.IsNullOrEmpty(strData))
                return "";

            var initialVectorBytes = Encoding.ASCII.GetBytes(InitVector);
			var saltValueBytes = Encoding.ASCII.GetBytes(Salt);
			var plainTextBytes = Encoding.UTF8.GetBytes(strData);
			var derivedPassword = new PasswordDeriveBytes(key, saltValueBytes, HashAlgo, PassIter);
#pragma warning disable 612,618
			var keyBytes = derivedPassword.GetBytes(KeySize / 8);
#pragma warning restore 612,618
			var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC };
	        byte[] cipherTextBytes;
			using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, initialVectorBytes))
            {
				using (var memStream = new MemoryStream())
                {
					using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }
            symmetricKey.Clear();
            return Convert.ToBase64String(cipherTextBytes);
        }


        public static string DecryptData(string strData, string key)
        {
            if (string.IsNullOrEmpty(strData))
                return "";

			var initialVectorBytes = Encoding.ASCII.GetBytes(InitVector);
			var saltValueBytes = Encoding.ASCII.GetBytes(Salt);
			var cipherTextBytes = Convert.FromBase64String(strData);
			var derivedPassword = new PasswordDeriveBytes(key, saltValueBytes, HashAlgo, PassIter);
#pragma warning disable 612,618
			var keyBytes = derivedPassword.GetBytes(KeySize / 8);
#pragma warning restore 612,618
			var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC };
	        var plainTextBytes = new byte[cipherTextBytes.Length];
			int byteCount;
			using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, initialVectorBytes))
            {
				using (var memStream = new MemoryStream(cipherTextBytes))
                {
					using (var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }
            symmetricKey.Clear();
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        } 
    }
} 
