using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Scripting.Compiler
{
    public class Branch
    {
        public string BranchName { get; private set; }
        public uint TextPosition { get; private set; }
        public ScriptExpression Condition { get; private set; }
        public ScriptExpression TargetScript { get; private set; }

        public Branch(string branchName, uint textPosition, ScriptExpression condition, ScriptExpression targetScript)
        {
            BranchName = branchName;
            TextPosition = textPosition;
            Condition = condition;
            TargetScript = targetScript;
        }
    }
}
