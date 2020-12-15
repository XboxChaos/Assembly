using Blamite.Serialization;
using Blamite.IO;
using System.Collections.Generic;

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
		public ushort Type { get; set; }

		public override bool Equals(object obj)
		{
			return obj is ScriptParameter parameter &&
				   Name == parameter.Name &&
				   Type == parameter.Type;
		}

		public override int GetHashCode()
		{
			var hashCode = -243844509;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
			hashCode = hashCode * -1521134295 + Type.GetHashCode();
			return hashCode;
		}

		public void Write(IWriter writer)
        {
            writer.WriteAscii(Name, 0x20);
            writer.WriteUInt16(Type);
            writer.WriteInt16(0);
        }

		private void Load(StructureValueCollection values)
		{
			Name = values.GetString("name");
			Type = (ushort) values.GetInteger("type");
		}


	}
}