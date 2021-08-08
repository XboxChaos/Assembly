using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     An expression in a script.
	/// </summary>
	public class ScriptExpression
	{
        #region constructors

        public ScriptExpression()
		{
		}

		private ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line)
		{
			Index = index;
			Opcode = opcode;
			ReturnType = valType;
			Type = expType;
			Next = DatumIndex.Null;
			StringOffset = strOffset;
			LineNumber = line;
		}

		// uint
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, uint value) : this (index, opcode, valType, expType, strOffset, line)
        {
			Value = new LongExpressionValue(value);
        }

		// short
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, ushort value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new ShortValue(value);
		}

		// short, short
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, ushort value1, ushort value2) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new LongExpressionValue(value1, value2);
		}

		// byte, byte, byte, byte
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, byte value1, byte value2, byte value3, byte value4) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new LongExpressionValue(value1, value2, value3, value4);
		}

		// byte
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, byte value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new ByteValue(value);
		}

		// float
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, float value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new FloatValue(value);
		}

		// StringID
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, StringID value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new LongExpressionValue(value);
		}

		// DatumIndex
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, DatumIndex value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new LongExpressionValue(value);
		}

		// Tag
		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, short line, ITag value) : this(index, opcode, valType, expType, strOffset, line)
		{
			Value = new LongExpressionValue(value);
		}


		internal ScriptExpression(StructureValueCollection values, ushort index, StringTableReader stringReader, int stringTableSize)
		{
			Load(values, index, stringReader, stringTableSize);
		}

        #endregion

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
		public ushort ReturnType { get; set; }

		/// <summary>
		///     Gets or sets the string associated with the expression. Can be null.
		/// </summary>
		public string StringValue { get; set; }

		/// <summary>
		///		Gets or sets the string offset of the expression.
		/// </summary>
		public uint StringOffset { get; set; }

		/// <summary>
		///     Gets or sets the value of the expression.
		/// </summary>
		public IExpressionValue Value { get; set; }

		/// <summary>
		///     Gets or sets the expression's line number, or 0 if the expression is implicit.
		/// </summary>
		public short LineNumber { get; set; }

		/// <summary>
		///     Gets or sets the expression to execute after this one. Can be null.
		/// </summary>
		public ScriptExpression NextExpression { get; set; }

		/// <summary>
		///		Gets or sets the datum index to the next expression.
		/// </summary>
		public DatumIndex Next { get; set; }

		/// <summary>
		/// Resolves the next expression.
		/// </summary>
		/// <param name="allExpressions">The map's script expression table.</param>
		internal void ResolveReferences(ScriptExpressionTable allExpressions)
		{
			if (Next.IsValid)
            {
				NextExpression = allExpressions.FindExpression(Next);
			}
		}

		/// <summary>
		/// Resolves the string value.
		/// </summary>
		/// <param name="requestedStrings">The table containing the expression strings.</param>
		internal void ResolveStrings(CachedStringTable requestedStrings)
		{
			StringValue = requestedStrings.GetString(StringOffset);
		}

		private void Load(StructureValueCollection values, ushort index, StringTableReader stringReader, int stringTableSize)
		{
			Index = new DatumIndex((ushort) values.GetInteger("datum index salt"), index);
			Opcode = (ushort) values.GetInteger("opcode");
			ReturnType = (ushort) values.GetInteger("value type");
			Type = (ScriptExpressionType) values.GetInteger("expression type");
			Next = new DatumIndex(values.GetInteger("next expression index"));
			StringOffset = (uint)values.GetIntegerOrDefault("string table offset", 0);
			Value = new LongExpressionValue((uint)values.GetInteger("value"));
			LineNumber = (short) values.GetIntegerOrDefault("source line", 1);

            if(StringOffset < stringTableSize)
            {
				stringReader.RequestString(StringOffset);
			}
		}

		/// <summary>
		///		Writes the expression to a stream.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		public void Write(IWriter writer)
		{
			writer.WriteUInt16(Index.Salt);
			writer.WriteUInt16(Opcode);
			writer.WriteUInt16(ReturnType);
			writer.WriteInt16((short)Type);
			writer.WriteUInt32(Next.Value);
			writer.WriteUInt32(StringOffset);
			Value.Write(writer);
			writer.WriteInt16(LineNumber);
			// zero could be part of the line number
			writer.WriteUInt16(0);
		}

		/// <summary>
		/// Clones a script expression.
		/// </summary>
		/// <returns>The cloned expression.</returns>
		public ScriptExpression Clone()
		{
			var clone = (ScriptExpression)this.MemberwiseClone();
			clone.Next = new DatumIndex(Next.Salt, Next.Index);
			return clone;
		}
	}
}