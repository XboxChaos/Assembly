namespace Blamite.Blam.Scripting
{
	public enum ScriptExpressionType : short
	{
		Group = 8,
		Expression = 9,
		ScriptReference = 10,
		GlobalsReference = 13,
		ParameterReference = 29,

		//hax for h4
		Group4 = 0x20,
		Expression4 = 0x21,
		ScriptReference4 = 0x22,
		VariableDecl4 = 0x24,
		GlobalsReference4 = 0x29,
		VariableReference4 = 0x39,
		ParameterReference4 = 0x69,
	}
}