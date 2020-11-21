using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting
{
    public interface IScriptingConstantInfo
    {
        string Name { get; }

        public ushort Opcode { get; }

        public string ReturnType { get; }
    }
}
