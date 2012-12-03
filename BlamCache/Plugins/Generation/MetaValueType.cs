using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Plugins.Generation
{
    public enum MetaValueType
    {
        TagReference,
        DataReference, // Data1 = Size, Pointer = Address
        Reflexive      // Data1 = Entry count, Pointer = Address
    }
}
