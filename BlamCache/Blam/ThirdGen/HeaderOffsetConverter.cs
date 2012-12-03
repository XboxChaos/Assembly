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
        private uint _magic;

        public HeaderOffsetConverter(uint stringMagicBase, int headerSize)
        {
            _magic = (uint)(stringMagicBase - headerSize);
        }

        public override uint PointerToOffset(uint pointer)
        {
            return pointer - _magic;
        }

        public override uint PointerToAddress(uint pointer)
        {
            throw new NotImplementedException();
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset + _magic;
        }

        public override uint AddressToPointer(uint address)
        {
            throw new NotImplementedException();
        }
    }
}
