using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtryzeDLL.Blam.Scripting
{
    public interface IScript
    {
        string Name { get; }
        IList<IScriptParameter> Parameters { get; }
        short ExecutionType { get; }
        short ReturnType { get; }
        IExpression RootExpression { get; }
    }
}
