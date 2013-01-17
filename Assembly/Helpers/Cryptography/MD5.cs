using System.Text;
using System.Security.Cryptography;

namespace Assembly.Helpers.Cryptography
{
    public class Md5Crypto
    {
        public static string ComputeHashToString(string str)
        {
			var enc = new ASCIIEncoding();
			var asciiText = enc.GetBytes(str);

            MD5 md5 = new MD5CryptoServiceProvider();
			var result = md5.ComputeHash(asciiText);

			var sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public static string ComputeHashToString(byte[] byteArr)
        {
			var md5 = new MD5CryptoServiceProvider();
			var result = md5.ComputeHash(byteArr);

			var sb = new StringBuilder();
			for (var i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static byte[] ComputeHashToByteArray(string str)
        {
			var enc = new ASCIIEncoding();
			var asciiText = enc.GetBytes(str);

            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(asciiText);
        }
        public static byte[] ComputeHashToByteArray(byte[] byteArr)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(byteArr);
        }
    }
} 
