using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.FirstGen
{
    class FirstGenPointerExpander : IPointerExpander
    {
        public bool IsValid { get { return false; } }//lol

        public long Expand(uint pointer)
        {
            return pointer;
        }

        public uint Contract(long pointer)
        {
            return (uint)pointer;
        }
    }
}