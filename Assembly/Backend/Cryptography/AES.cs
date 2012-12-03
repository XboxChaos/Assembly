using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Assembly.Backend.Cryptography
{
    public class AESCrypto
    {
        private static string _salt = "asm__";
        private static string _hashAlgo = "SHA1";
        private static int _passIter = 2;
        private static string _initVector = "OFRna73m*aze01xY";
        private static int _keySize = 256;

        public static string EncryptData(string strData, string key)
        {
            if (string.IsNullOrEmpty(strData))
                return "";

            byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(_initVector);
            byte[] SaltValueBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] PlainTextBytes = Encoding.UTF8.GetBytes(strData);
            PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(key, SaltValueBytes, _hashAlgo, _passIter);
            byte[] KeyBytes = DerivedPassword.GetBytes(_keySize / 8);
            RijndaelManaged SymmetricKey = new RijndaelManaged();
            SymmetricKey.Mode = CipherMode.CBC;
            byte[] CipherTextBytes = null;
            using (ICryptoTransform Encryptor = SymmetricKey.CreateEncryptor(KeyBytes, InitialVectorBytes))
            {
                using (MemoryStream MemStream = new MemoryStream())
                {
                    using (CryptoStream CryptoStream = new CryptoStream(MemStream, Encryptor, CryptoStreamMode.Write))
                    {
                        CryptoStream.Write(PlainTextBytes, 0, PlainTextBytes.Length);
                        CryptoStream.FlushFinalBlock();
                        CipherTextBytes = MemStream.ToArray();
                        MemStream.Close();
                        CryptoStream.Close();
                    }
                }
            }
            SymmetricKey.Clear();
            return Convert.ToBase64String(CipherTextBytes);
        }


        public static string DecryptData(string strData, string key)
        {
            if (string.IsNullOrEmpty(strData))
                return "";

            byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(_initVector);
            byte[] SaltValueBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] CipherTextBytes = Convert.FromBase64String(strData);
            PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(key, SaltValueBytes, _hashAlgo, _passIter);
            byte[] KeyBytes = DerivedPassword.GetBytes(_keySize / 8);
            RijndaelManaged SymmetricKey = new RijndaelManaged();
            SymmetricKey.Mode = CipherMode.CBC;
            byte[] PlainTextBytes = new byte[CipherTextBytes.Length];
            int ByteCount = 0;
            using (ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes))
            {
                using (MemoryStream MemStream = new MemoryStream(CipherTextBytes))
                {
                    using (CryptoStream CryptoStream = new CryptoStream(MemStream, Decryptor, CryptoStreamMode.Read))
                    {
                        ByteCount = CryptoStream.Read(PlainTextBytes, 0, PlainTextBytes.Length);
                        MemStream.Close();
                        CryptoStream.Close();
                    }
                }
            }
            SymmetricKey.Clear();
            return Encoding.UTF8.GetString(PlainTextBytes, 0, ByteCount);
        } 
    }
} 
