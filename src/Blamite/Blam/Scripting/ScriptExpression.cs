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
		private DatumIndex _nextIndex = DatumIndex.Null;
		private int _stringTableOffset;

		public ScriptExpression()
		{
		}

		public ScriptExpression(DatumIndex index, UInt16 opcode, Int16 valType, ScriptExpressionType expType, 
			DatumIndex nextExp, int strOffset, object value, Int16 line)
		{
			Index = index;
			Opcode = opcode;
			ReturnType = valType;
			Type = expType;
			_nextIndex = nextExp;
			_stringTableOffset = strOffset;
			ConvertValue(value);
			LineNumber = line;
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

		private void Load(StructureValueCollection values, ushort index, StringTableReader stringReader, int stringTableSize)
		{
			Index = new DatumIndex((ushort) values.GetInteger("datum index salt"), index);
			Opcode = (ushort) values.GetInteger("opcode");
			ReturnType = (short) values.GetInteger("value type");
			Type = (ScriptExpressionType) values.GetInteger("expression type");
			_nextIndex = new DatumIndex(values.GetInteger("next expression index"));
			_stringTableOffset = (int) values.GetIntegerOrDefault("string table offset", 0);
			Value = (uint)values.GetInteger("value");
			LineNumber = (short) values.GetIntegerOrDefault("source line", 1);

            if(_stringTableOffset < stringTableSize)
			    stringReader.RequestString(_stringTableOffset);
		}

		/// <summary>
		///		Writes the expression to a stream.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		public void Write(IWriter writer)
		{
			writer.WriteUInt16(Index.Salt);
			writer.WriteUInt16(Opcode);
			writer.WriteInt16(ReturnType);
			writer.WriteInt16((short)Type);
			writer.WriteUInt32(_nextIndex.Value);
			writer.WriteInt32(_stringTableOffset);
			writer.WriteUInt32(Value);
			writer.WriteInt16(LineNumber);
		}

		private void ConvertValue(object data)
		{
			uint result = 0xFFFFFFFF;
			switch(data)
			{
				case uint u32:
					result = u32;
					break;

				case ushort u16:
					result = (uint)(u16 << 16) | 0xFFFF;
					break;

				case ushort[] u16Arr:
					result = UInt16ArrToValue(u16Arr);
					break;

				case byte by:
					result = (uint)(by << 24) | 0xFFFFFF;
					break;

				case byte[] byArr:
					result = ByteArrToValue(byArr);
					break;

				case StringID sid:
					result = sid.Value;
					break;

				case DatumIndex datum:
					result = datum.Value;
					break;

				case ITag tag:
					result = tag.Index.Value;
					break;

				default:
					throw new NotImplementedException($"Unable to convert an object of the type {data.GetType()} " +
						$"to an expression value.");
			}

			Value = result;
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
				result = (uint)(data[0] << 16) | 0xFFFF;
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
					result = upper | 0xFFFF;
					break;

				case 1:
					result = (uint)data[0] << 24 | 0xFFFFFF;
					break;

				default:
					throw new ArgumentNullException($"Unable to convert a byte array of the length {len} to an expression value.");
			}

			return result;
		}

	}
}