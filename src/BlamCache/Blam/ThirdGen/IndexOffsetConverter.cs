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
            throw new NotSupportedException();
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset - _header.LocaleOffsetMask;
        }

        public override uint AddressToPointer(uint address)
        {
            throw new NotSupportedException();
        }

        public override bool SupportsAddresses
        {
            get { return false; }
        }

        public override bool SupportsOffsets
        {
            get { return true; }
        }
    }
}
