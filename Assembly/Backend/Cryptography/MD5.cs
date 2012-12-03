using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Assembly.Backend.Cryptography
{
    public class MD5Crypto
    {
        public static string ComputeHashToString(string str)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] ASCIIText = new byte[str.Length * 2];
            ASCIIText = enc.GetBytes(str);

            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(ASCIIText);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }
        public static string ComputeHashToString(byte[] byteArr)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(byteArr);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static byte[] ComputeHashToBA(string str)
        {
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] ASCIIText = new byte[str.Length * 2];
            ASCIIText = enc.GetBytes(str);

            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(ASCIIText);
        }
        public static byte[] ComputeHashToBA(byte[] byteArr)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(byteArr);
        }
    }
} 
