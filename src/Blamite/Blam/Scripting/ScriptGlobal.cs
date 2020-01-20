using Blamite.Serialization;
using Blamite.IO;

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

		internal ScriptGlobal(StructureValueCollection values, StringIDSource stringIDs)
		{
			Load(values, stringIDs);
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

		private void Load(StructureValueCollection values, StringIDSource stringIDs)
		{
			Name = values.HasInteger("name index")
				? stringIDs.GetString(new StringID(values.GetInteger("name index")))
				: values.GetString("name");

			Type = (short) values.GetInteger("type");
			ExpressionIndex = new DatumIndex(values.GetInteger("expression index"));
		}

        public void Write(IWriter writer)
        {
            writer.WriteAscii(Name, 0x20);
            writer.WriteInt16(Type);
            writer.WriteInt16(0);
            writer.WriteUInt32(ExpressionIndex.Value);
        }
	}
}