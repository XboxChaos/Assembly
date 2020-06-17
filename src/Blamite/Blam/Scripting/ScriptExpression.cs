using Blamite.Serialization;
using Blamite.IO;
using System;

namespace Blamite.Blam.Scripting
{
	/// <summary>
	///     An expression in a script.
	/// </summary>
	public class ScriptExpression
	{
		public ScriptExpression()
		{
		}

		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType,
			uint strOffset, object value, Int16 line)
		{
			Index = index;
			Opcode = opcode;
			ReturnType = valType;
			Type = expType;
			Next = DatumIndex.Null;
			StringOffset = strOffset;
			SetValue(value);
			LineNumber = line;
		}

		public ScriptExpression(DatumIndex index, ushort opcode, ushort valType, ScriptExpressionType expType, 
			DatumIndex nextExp, uint strOffset, object value, Int16 line) : this(index, opcode, valType, expType, strOffset, value, line)
		{
			Next = nextExp;
		}

		internal ScriptExpression(StructureValueCollection values, ushort index, StringTableReader stringReader, int stringTableSize)
		{
			Load(values, index, stringReader, stringTableSize);
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
		public uint Value { get; set; }

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

		internal void ResolveReferences(ScriptExpressionTable allExpressions)
		{
			if (Next.IsValid)
				NextExpression = allExpressions.FindExpression(Next);
		}

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
			Value = (uint)values.GetInteger("value");
			LineNumber = (short) values.GetIntegerOrDefault("source line", 1);

            if(StringOffset < stringTableSize)
			    stringReader.RequestString(StringOffset);
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
			writer.WriteUInt32(Value);
			writer.WriteInt16(LineNumber);
			// zero could be part of the line number
			writer.WriteUInt16(0);
		}

		public void SetValue(object data)
		{
			uint result = data switch
			{
				uint u32 => u32,
				ushort u16 => (uint)(0xFFFF << 16 | u16),
				ushort[] u16Arr => UInt16ArrToValue(u16Arr),
				byte by => (uint)(0xFFFFFF << 8 | by),
				byte[] byArr => ByteArrToValue(byArr),
				float fl => BitConverter.ToUInt32(BitConverter.GetBytes(fl), 0),
				StringID sid => sid.Value,
				DatumIndex datum => datum.Value,
				ITag tag => tag.Index.Value,
				_ => throw new NotImplementedException($"Unable to convert an object of the type {data.GetType()} " +
					$"to an expression value."),
			};
			Value = result;
		}

		public ScriptExpression Clone()
		{
			var clone = (ScriptExpression)this.MemberwiseClone();
			clone.Next = new DatumIndex(Next.Salt, Next.Index);
			return clone;
		}

		private uint UInt16ArrToValue(ushort[] data)
		{
			int len = data.Length;
			uint result;

			if (len == 2)
			{
				result = (uint)(data[0] << 16) | data[1];
			}
			else if (len == 1)
			{
				result = (uint)(data[0] << 16 | 0xFFFF);
			}
			else
				throw new ArgumentException("Unable to convert the array to an expression value.");

			return result;
		}

		private uint ByteArrToValue(byte[] data)
		{
			int len = data.Length;
			uint upper;
			uint lower;

			uint result;

			switch (len)
			{
				case 4:
					upper = (uint)(data[0] << 24 | data[1] << 16);
					lower = (uint)(data[2] << 8 | data[3]);
					result = upper | lower;
					break;

				case 3:
					upper = (uint)(data[0] << 24 | data[1] << 16);
					lower = (uint)(data[2] << 8 | 0xFF);
					result = upper | lower;
					break;

				case 2:
					upper = (uint)(data[0] << 24 | data[1] << 16);
					lower = 0xFFFF;
					result = upper | lower;
					break;

				case 1:
					upper = (uint)(data[0] << 24 | 0xFF << 16);
					lower = 0xFFFF;
					result = upper | lower;
					break;

				default:
					throw new ArgumentNullException($"Unable to convert a byte array of the length {len} to an expression value.");
			}

			return result;
		}

	}
}