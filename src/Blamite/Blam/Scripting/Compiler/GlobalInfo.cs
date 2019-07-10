using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Compiler
{
    public class GlobalInfo
    {
        public string Name { get; private set; }
        public string ValueType { get; private set; }

        public GlobalInfo(BS_ReachParser.GloDeclContext context)
        {
            Name = context.ID().GetText();
            ValueType = context.VALUETYPE().GetText();
        }

        //public void Print()
        //{
        //    Console.WriteLine($"Global - Name: {Name} ValueType: {ValueType}");
        //}
    }
}
