using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.SecondGen.Structures;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.SecondGen
{
    public class MetaOffsetConverter : PointerConverter
    {
        private SecondGenHeader _header;

        public MetaOffsetConverter(SecondGenHeader header)
        {
            _header = header;
        }

        public override uint PointerToOffset(uint pointer)
        {
            return pointer - _header.MetaOffsetMask + _header.MetaOffset;
        }

        public override uint PointerToAddress(uint pointer)
        {
            throw new NotSupportedException();
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset - _header.MetaOffset + _header.MetaOffsetMask;
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
