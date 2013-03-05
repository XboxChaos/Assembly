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
        SegmentPointer ScriptExpressionsLocation { get; set; }
        SegmentPointer ScriptGlobalsLocation { get; set; }
        SegmentPointer ScriptObjectsLocation { get; set; }
        SegmentPointer ScriptsLocation { get; set; }

        ExpressionTable ScriptExpressions { get; }
        List<IGlobal> ScriptGlobals { get; }
        List<IGlobalObject> ScriptObjects { get; }
        List<IScript> Scripts { get; }
    }
}
