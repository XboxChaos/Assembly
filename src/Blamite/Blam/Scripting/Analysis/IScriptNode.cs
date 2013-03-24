using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Analysis
{
    public interface IScriptNode
    {
        void Accept(IScriptNodeVisitor visitor);
    }
}
