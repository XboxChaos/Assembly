using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Util
{
    public class Adler32
    {
        const uint MOD_ADLER = 65521;

        // TODO (Dragon): not very efficient
        public static uint Calculate(byte[] data)
        {
            uint a = 1;
            uint b = 0;

            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }
    }
}
