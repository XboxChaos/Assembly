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