using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace ExtryzeDLL.Util
{
    public static class AES
    {
        public static byte[] Decrypt(byte[] encryptedBytes, byte[] key, byte[] IV)
        {
            AesCryptoServiceProvider crypto = new AesCryptoServiceProvider();
            crypto.Key = key;
            crypto.IV = IV;
            crypto.Padding = PaddingMode.None;
            ICryptoTransform transformer = crypto.CreateDecryptor();
            return transformer.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        }
    }
}
