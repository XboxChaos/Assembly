using Blamite.Flexibility;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     A global variable in a script file.
	/// </summary>
	public class ScriptGlobal
	{
		public ScriptGlobal()
		{
		}

		internal ScriptGlobal(StructureValueCollection values)
		{
			Load(values);
		}

		/// <summary>
		///     Gets or sets the name of the global variable.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     Gets or sets the type of the global variable.
		/// </summary>
		public short Type { get; set; }

		/// <summary>
		///     Gets or sets the datum index of the expression which determines the variable's default value.
		/// </summary>
		public DatumIndex ExpressionIndex { get; set; }

		private void Load(StructureValueCollection values)
		{
			Name = values.GetString("name");
			Type = (short) values.GetInteger("type");
			ExpressionIndex = new DatumIndex(values.GetInteger("expression index"));
		}
	}
}