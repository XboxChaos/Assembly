using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Scripting.Analysis
{
    public interface IScriptNodeVisitor
    {
        void VisitGlobalDefinition(GlobalDefinitionNode node);
        void VisitScriptDefinition(ScriptDefinitionNode node);
        void VisitFunctionCall(FunctionCallNode node);
        void VisitConstant(ConstantNode node);
        void VisitVariableReference(VariableReferenceNode node);
    }
}
