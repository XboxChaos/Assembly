using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Assembly.Backend.Cryptography
{
    public class RSACrypto
    {
        private static RSACryptoServiceProvider m_rsaProvider = new RSACryptoServiceProvider(2048);
        
        public static string EncryptData(string strData2Encrypt, string publickey)
        {
            string FullKey = publickey.Substring(31);
            m_rsaProvider.FromXmlString(FullKey);
            byte[] byteData = Encoding.UTF32.GetBytes(strData2Encrypt);
            int maxLength = 214;
            int dataLength = byteData.Length;
            int iterations = dataLength / maxLength;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= iterations; i++)
            {
                byte[] tempBytes = new byte[(dataLength - maxLength * i > maxLength) ? maxLength : dataLength - maxLength * i];
                Buffer.BlockCopy(byteData, maxLength * i, tempBytes, 0, tempBytes.Length);

                byte[] EncbyteData = m_rsaProvider.Encrypt(tempBytes, false);
                sb.Append(Convert.ToBase64String(EncbyteData));
            }
            return sb.ToString();
        }


        public static string DecryptData(string strData2Decrypt, string privatekey)
        {
            string FullKey = privatekey.Substring(31);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(FullKey);
            int base64BlockSize = (256 % 3 != 0) ? ((256 / 3) * 4) + 4 : (256 / 3) * 4;
            int iterations = strData2Decrypt.Length / base64BlockSize;
            int l = 0;
            byte[] fullbytes = new byte[0];
            for (int i = 0; i < iterations; i++)
            {
                byte[] encBytes = Convert.FromBase64String(strData2Decrypt.Substring(base64BlockSize * i, base64BlockSize));
                byte[] bytes = rsa.Decrypt(encBytes, false);
                Array.Resize(ref fullbytes, fullbytes.Length + bytes.Length);
                for (int k = 0; k < bytes.Length; k++)
                {
                    fullbytes[l] = bytes[k];
                    l++;
                }
            }

            return Encoding.UTF32.GetString(fullbytes);
        } 
    }
} 
