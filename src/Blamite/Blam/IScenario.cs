using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Scripting;
using Blamite.Blam.Util;
using Blamite.IO;

namespace Blamite.Blam
{
    public interface IScenario
    {
        ScriptExpressionTable ScriptExpressions { get; }
        List<ScriptGlobal> ScriptGlobals { get; }
        List<ScriptObjectReference> ScriptObjects { get; }
        List<Script> Scripts { get; }
    }
}
