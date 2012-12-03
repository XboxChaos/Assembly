using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    public class IndexOffsetConverter : PointerConverter
    {
        private ThirdGenHeader _header;

        public IndexOffsetConverter(ThirdGenHeader header)
        {
            _header = header;
        }

        public override uint PointerToOffset(uint pointer)
        {
            return pointer + _header.LocaleOffsetMask;
        }

        public override uint PointerToAddress(uint pointer)
        {
            throw new InvalidOperationException("Third-generation index offsets cannot yet be converted to addresses.");
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset - _header.LocaleOffsetMask;
        }

        public override uint AddressToPointer(uint address)
        {
            throw new InvalidOperationException("Addresses cannot yet be converted to third-generation index offsets.");
        }
    }
}
