using Blamite.Flexibility;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     A parameter for a script.
	/// </summary>
	public class ScriptParameter
	{
		public ScriptParameter()
		{
		}

		internal ScriptParameter(StructureValueCollection values)
		{
			Load(values);
		}

		/// <summary>
		///     Gets or sets the name of the parameter.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     Gets or sets the type opcode of the parameter.
		/// </summary>
		public short Type { get; set; }

		private void Load(StructureValueCollection values)
		{
			Name = values.GetString("name");
			Type = (short) values.GetInteger("type");
		}
	}
}