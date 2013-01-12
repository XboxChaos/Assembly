using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ExtryzeDLL.Util
{
    public static class SHA1
    {
        public static byte[] Transform(byte[] data)
        {
            return _sha1.ComputeHash(data);
        }

        private static SHA1CryptoServiceProvider _sha1 = new SHA1CryptoServiceProvider();
    }
}
