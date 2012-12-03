using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam
{
    public interface IScenario
    {
        Pointer ScriptExpressionsLocation { get; set; }
        Pointer ScriptGlobalsLocation { get; set; }
        Pointer ScriptObjectsLocation { get; set; }
        Pointer ScriptsLocation { get; set; }

        ExpressionTable ScriptExpressions { get; }
        List<IGlobal> ScriptGlobals { get; }
        List<IGlobalObject> ScriptObjects { get; }
        List<IScript> Scripts { get; }
    }
}
