using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Context
{
    [Serializable]
    public class ScriptingContextException : Exception
    {
        public ScriptingContextException()
        { }

        public ScriptingContextException(string message)
            : base(message)
        { }

        public ScriptingContextException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
