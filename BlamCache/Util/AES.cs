using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace ExtryzeDLL.Util
{
    public static class AES
    {
        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider crypto = new AesCryptoServiceProvider();
            crypto.Key = key;
            crypto.IV = iv;
            crypto.Padding = PaddingMode.None;
            ICryptoTransform transformer = crypto.CreateDecryptor();
            return transformer.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Decrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider crypto = new AesCryptoServiceProvider();
            crypto.Key = key;
            crypto.IV = iv;
            crypto.Padding = PaddingMode.None;
            ICryptoTransform transformer = crypto.CreateDecryptor();
            return transformer.TransformFinalBlock(data, offset, length);
        }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider crypto = new AesCryptoServiceProvider();
            crypto.Key = key;
            crypto.IV = iv;
            crypto.Padding = PaddingMode.None;
            ICryptoTransform transformer = crypto.CreateEncryptor();
            return transformer.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Encrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
        {
            AesCryptoServiceProvider crypto = new AesCryptoServiceProvider();
            crypto.Key = key;
            crypto.IV = iv;
            crypto.Padding = PaddingMode.None;
            ICryptoTransform transformer = crypto.CreateEncryptor();
            return transformer.TransformFinalBlock(data, offset, length);
        }
    }
}
