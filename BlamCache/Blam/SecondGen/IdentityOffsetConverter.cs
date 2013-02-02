using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.SecondGen
{
    /// <summary>
    /// A PointerConverter which only accepts offsets and applies no conversions to them.
    /// </summary>
    public class IdentityOffsetConverter : PointerConverter
    {
        public override uint PointerToOffset(uint pointer)
        {
            return pointer;
        }

        public override uint PointerToAddress(uint pointer)
        {
            throw new NotSupportedException();
        }

        public override uint OffsetToPointer(uint offset)
        {
            return offset;
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
