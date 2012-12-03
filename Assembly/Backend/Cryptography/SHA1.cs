using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Assembly.Backend.Cryptography
{
    public class SHA1Crypto
    {
        public static string ComputeHashToString(string str)
        {
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] result = sha1.ComputeHash(unicodeText);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public static string ComputeHashToString(byte[] byteArr)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] result = sha1.ComputeHash(byteArr);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static byte[] ComputeHashToBA(string str)
        {
            Encoder enc = System.Text.Encoding.Unicode.GetEncoder();
            byte[] unicodeText = new byte[str.Length * 2];
            enc.GetBytes(str.ToCharArray(), 0, str.Length, unicodeText, 0, true);

            SHA1 sha1 = new SHA1CryptoServiceProvider();
            return sha1.ComputeHash(unicodeText);
        }
        public static byte[] ComputeHashToBA(byte[] byteArr)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            return sha1.ComputeHash(byteArr);
        }
    }
} 
