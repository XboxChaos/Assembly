using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// Converts file offsets to pointers by adding a mask value
    /// and converts pointers to file offsets by subtracting it.
    /// </summary>
    public class MaskedPointerConverter : IPointerConverter
    {
        private uint _baseMask;

        public MaskedPointerConverter(uint mask)
        {
            _baseMask = mask;
        }

        public int PointerToOffset(uint pointer)
        {
            return (int)(pointer - _baseMask);
        }

        public uint OffsetToPointer(int offset)
        {
            return (uint)(offset + _baseMask);
        }
    }
}
