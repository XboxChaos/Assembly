namespace Blamite.Blam.Scripting.Analysis
{
	public interface IScriptNode
	{
		void Accept(IScriptNodeVisitor visitor);
	}
}