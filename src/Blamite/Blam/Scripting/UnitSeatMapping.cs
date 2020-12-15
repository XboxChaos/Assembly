using System;

namespace Blamite.Blam.Scripting
{
    public class UnitSeatMapping
    {
        public short Count { get; private set; }
        public short Index { get; private set; }
        public string Name { get; private set; }

        public UnitSeatMapping(short index, short count, string name)
        {
            Count = count;
            Index = index;
            Name = name;
        }
    }
}
