using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting
{
    public interface IScriptParameter
    {
        string Name { get; }
        short Type { get; }
    }
}
