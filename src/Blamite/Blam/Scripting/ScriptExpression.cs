using Blamite.Flexibility;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     An expression in a script.
	/// </summary>
	public class ScriptExpression
	{
		private DatumIndex _nextIndex = DatumIndex.Null;
		private int _stringTableOffset;

		public ScriptExpression()
		{
		}

		internal ScriptExpression(StructureValueCollection values, ushort index, StringTableReader stringReader)
		{
			Load(values, index, stringReader);
		}

		/// <summary>
		///     Gets or sets the expression's datum index.
		/// </summary>
		public DatumIndex Index { get; set; }

		/// <summary>
		///     Gets or sets the opcode associated with the expression.
		/// </summary>
		public ushort Opcode { get; set; }

		/// <summary>
		///     Gets or sets the type of the expression.
		/// </summary>
		public ScriptExpressionType Type { get; set; }

		/// <summary>
		///     Gets or sets the type of the expression's return value.
		/// </summary>
		public short ReturnType { get; set; }

		/// <summary>
		///     Gets or sets the string associated with the expression. Can be null.
		/// </summary>
		public string StringValue { get; set; }

		/// <summary>
		///     Gets or sets the value of the expression.
		/// </summary>
		public uint Value { get; set; }

		/// <summary>
		///     Gets or sets the expression's line number, or 0 if the expression is implicit.
		/// </summary>
		public short LineNumber { get; set; }

		/// <summary>
		///     Gets or sets the expression to execute after this one. Can be null.
		/// </summary>
		public ScriptExpression Next { get; set; }

		internal void ResolveReferences(ScriptExpressionTable allExpressions)
		{
			if (_nextIndex.IsValid)
				Next = allExpressions.FindExpression(_nextIndex);
		}

		internal void ResolveStrings(CachedStringTable requestedStrings)
		{
			StringValue = requestedStrings.GetString(_stringTableOffset);
		}

		private void Load(StructureValueCollection values, ushort index, StringTableReader stringReader)
		{
			Index = new DatumIndex((ushort) values.GetInteger("datum index salt"), index);
			Opcode = (ushort) values.GetInteger("opcode");
			ReturnType = (short) values.GetInteger("value type");
			Type = (ScriptExpressionType) values.GetInteger("expression type");
			_nextIndex = new DatumIndex(values.GetInteger("next expression index"));
			_stringTableOffset = (int) values.GetIntegerOrDefault("string table offset", 0);
			Value = values.GetInteger("value");
			LineNumber = (short) values.GetIntegerOrDefault("source line", 0);

			stringReader.RequestString(_stringTableOffset);
		}
	}
}