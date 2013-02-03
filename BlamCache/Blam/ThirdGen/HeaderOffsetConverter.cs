using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    public class HeaderOffsetConverter : PointerConverter
    {
        private ThirdGenHeader _header;

        public HeaderOffsetConverter(ThirdGenHeader header)
        {
            _header = header;
        }

        public override uint PointerToOffset(uint pointer)
        {
            return pointer - (uint)(_header.StringOffsetMagic - _header.HeaderSize);
        }

        public override uint PointerToAddress(uint pointer)
        {
            throw new NotSupportedException();
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset + (uint)(_header.StringOffsetMagic - _header.HeaderSize);
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
