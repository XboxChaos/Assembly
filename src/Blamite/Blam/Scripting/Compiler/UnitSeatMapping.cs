using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class UnitSeatMapping
    {
        public Int16 Count { get; private set; }
        public Int16 Index { get; private set; }
        public string Name { get; private set; }

        public UnitSeatMapping(Int16 index, Int16 count, string name)
        {
            Count = count;
            Index = index;
            Name = name;
        }
    }
}
