using System;
using System.Text;

namespace Blamite.Blam
{
    public static class SaltGenerator
    {
        public static ushort GetSalt(string tableName)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(tableName);
            uint c1 = bytes[0];
            uint c2 = bytes[1];

            c2 = c2 | 0x80;
            c2 = c2 << 8;

            return (ushort)(c2 | c1);
        }
    }
}
