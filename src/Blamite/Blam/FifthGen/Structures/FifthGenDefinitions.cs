using System.Collections.Generic;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     A field type as a payload declares it, giving the type's name and its on-disk width.
	/// </summary>
	public class FifthGenTypeDefinition
	{
		internal FifthGenTypeDefinition(uint nameOffset, uint size, uint flags)
		{
			NameOffset = nameOffset;
			Size = size;
			Flags = flags;
		}

		/// <summary>
		///     Gets the byte offset of the type's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the type's name, e.g. <c>real point 3d</c>.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the type's on-disk width in bytes, as the payload declares it. This is the authoritative width.
		/// </summary>
		public uint Size { get; private set; }

		/// <summary>
		///     Gets the type's flags. Nothing is known about their meaning; they are preserved as read.
		/// </summary>
		public uint Flags { get; private set; }

		/// <summary>
		///     Gets the known type the name resolves to, or <see cref="FifthGenFieldType.Unknown" />.
		/// </summary>
		public FifthGenFieldType Type { get; internal set; }

		public override string ToString()
		{
			return $"{Name} ({Size} byte(s))";
		}
	}

	/// <summary>
	///     A single field of a struct definition.
	/// </summary>
	/// <remarks>
	///     The field table is one flat list for the whole tag, with each struct's fields as a contiguous run delimited by a
	///     terminator field. A struct's run is located by reading the first-field index out of the struct record rather than by
	///     working backwards from where the previous struct's run ended, because the runs are not always laid out in struct
	///     order.
	/// </remarks>
	public class FifthGenFieldDefinition
	{
		internal FifthGenFieldDefinition(int index, uint nameOffset, uint typeIndex, uint aux)
		{
			Index = index;
			NameOffset = nameOffset;
			TypeIndex = typeIndex;
			Aux = aux;
		}

		/// <summary>
		///     Gets the field's index in the layout's field table.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the byte offset of the field's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the field's name. Empty for padding and terminator fields.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the index of the field's type in the layout's type table.
		/// </summary>
		public uint TypeIndex { get; private set; }

		/// <summary>
		///     Gets the field's type as declared by the payload.
		/// </summary>
		public FifthGenTypeDefinition TypeDefinition { get; internal set; }

		/// <summary>
		///     Gets the field's known type, or <see cref="FifthGenFieldType.Unknown" />.
		/// </summary>
		public FifthGenFieldType Type
		{
			get { return (TypeDefinition != null) ? TypeDefinition.Type : FifthGenFieldType.Unknown; }
		}

		/// <summary>
		///     Gets the field's type name.
		/// </summary>
		public string TypeName
		{
			get { return (TypeDefinition != null) ? TypeDefinition.Name : null; }
		}

		/// <summary>
		///     Gets the field's auxiliary word, whose meaning depends on the field's type.
		/// </summary>
		public uint Aux { get; private set; }

		/// <summary>
		///     Gets what the field's auxiliary word means.
		/// </summary>
		public FifthGenAuxKind AuxKind { get; internal set; }

		/// <summary>
		///     Gets the number of bytes the field occupies inline in its struct's packed data.
		/// </summary>
		public int InlineSize { get; internal set; }

		/// <summary>
		///     Gets the struct the field inlines, for a struct field. Null otherwise.
		/// </summary>
		public FifthGenStructDefinition Struct { get; internal set; }

		/// <summary>
		///     Gets the array the field repeats, for an array field. Null otherwise.
		/// </summary>
		public FifthGenArrayDefinition Array { get; internal set; }

		/// <summary>
		///     Gets the block the field points at, for a block or block index field. Null if it did not resolve.
		/// </summary>
		public FifthGenBlockDefinition Block { get; internal set; }

		/// <summary>
		///     Gets the enumeration the field's value is drawn from, for an enumeration or flags field. Null if it did not resolve.
		/// </summary>
		public FifthGenEnumDefinition Enum { get; internal set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}'";
		}
	}

	/// <summary>
	///     A struct definition: a contiguous run of fields, addressed by the index of its first one.
	/// </summary>
	public class FifthGenStructDefinition
	{
		internal FifthGenStructDefinition(int index, byte[] guid, uint nameOffset, int firstField, uint aux)
		{
			Index = index;
			Guid = guid;
			NameOffset = nameOffset;
			FirstField = firstField;
			Aux = aux;
			Fields = new List<FifthGenFieldDefinition>();
			InlineSize = -1;
		}

		/// <summary>
		///     Gets the struct's index in the layout's struct table.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the struct's sixteen byte identifier, preserved as read.
		/// </summary>
		public byte[] Guid { get; private set; }

		/// <summary>
		///     Gets the byte offset of the struct's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the struct's name.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the index of the struct's first field in the layout's field table.
		/// </summary>
		public int FirstField { get; private set; }

		/// <summary>
		///     Gets the struct's auxiliary word. Nothing is known about its meaning.
		/// </summary>
		public uint Aux { get; private set; }

		/// <summary>
		///     Gets the struct's fields, in declaration order. The run's terminator field is not included.
		/// </summary>
		public IList<FifthGenFieldDefinition> Fields { get; internal set; }

		/// <summary>
		///     Gets the terminator field that closes the struct's run in the field table.
		/// </summary>
		public FifthGenFieldDefinition Terminator { get; internal set; }

		/// <summary>
		///     Gets the number of bytes one instance of the struct occupies in packed element data.
		/// </summary>
		public int InlineSize { get; internal set; }

		/// <summary>
		///     Gets whether any of the struct's fields, directly or through an inline array, contributes a nested section.
		/// </summary>
		/// <remarks>
		///     A struct with nothing to contribute still gets a wrapper, so this does not predict whether a wrapper exists. It
		///     says whether an empty one is unremarkable.
		/// </remarks>
		public bool EmitsSections { get; internal set; }

		public override string ToString()
		{
			return $"{Name} ({Fields.Count} field(s), {InlineSize} byte(s))";
		}
	}

	/// <summary>
	///     A block definition: a variable-length list of elements of one struct.
	/// </summary>
	public class FifthGenBlockDefinition
	{
		internal FifthGenBlockDefinition(int index, uint nameOffset, uint maxCount, uint structIndex)
		{
			Index = index;
			NameOffset = nameOffset;
			MaxCount = maxCount;
			StructIndex = structIndex;
		}

		/// <summary>
		///     Gets the block's index in the layout's block table.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the byte offset of the block's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the block's name.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the maximum number of elements the editor allows in the block. This is an authoring limit, not a statement
		///     about the data.
		/// </summary>
		public uint MaxCount { get; private set; }

		/// <summary>
		///     Gets the index of the struct one element of the block is laid out as.
		/// </summary>
		public uint StructIndex { get; private set; }

		/// <summary>
		///     Gets the struct one element of the block is laid out as.
		/// </summary>
		public FifthGenStructDefinition Struct { get; internal set; }

		public override string ToString()
		{
			return $"{Name} (max {MaxCount})";
		}
	}

	/// <summary>
	///     An inline fixed-length array of structs.
	/// </summary>
	public class FifthGenArrayDefinition
	{
		internal FifthGenArrayDefinition(int index, uint nameOffset, uint count, uint structIndex)
		{
			Index = index;
			NameOffset = nameOffset;
			Count = count;
			StructIndex = structIndex;
		}

		/// <summary>
		///     Gets the array's index in the layout's array table.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the byte offset of the array's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the array's name.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the number of elements the array always has.
		/// </summary>
		public uint Count { get; private set; }

		/// <summary>
		///     Gets the index of the struct one element of the array is laid out as.
		/// </summary>
		public uint StructIndex { get; private set; }

		/// <summary>
		///     Gets the struct one element of the array is laid out as.
		/// </summary>
		public FifthGenStructDefinition Struct { get; internal set; }

		public override string ToString()
		{
			return $"{Name}[{Count}]";
		}
	}

	/// <summary>
	///     An enumeration or flags definition, whose option names are a run in the layout's flat option list.
	/// </summary>
	public class FifthGenEnumDefinition
	{
		internal FifthGenEnumDefinition(int index, uint nameOffset, uint optionCount, uint firstOption)
		{
			Index = index;
			NameOffset = nameOffset;
			OptionCount = optionCount;
			FirstOption = firstOption;
			Options = new List<string>();
		}

		/// <summary>
		///     Gets the enumeration's index in the layout's enumeration table.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		///     Gets the byte offset of the enumeration's name in the layout's name blob.
		/// </summary>
		public uint NameOffset { get; private set; }

		/// <summary>
		///     Gets the enumeration's name.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		///     Gets the number of options the enumeration has.
		/// </summary>
		public uint OptionCount { get; private set; }

		/// <summary>
		///     Gets the index of the enumeration's first option in the layout's flat option list.
		/// </summary>
		public uint FirstOption { get; private set; }

		/// <summary>
		///     Gets the enumeration's option names, in declaration order.
		/// </summary>
		public IList<string> Options { get; internal set; }

		public override string ToString()
		{
			return $"{Name} ({OptionCount} option(s))";
		}
	}

	/// <summary>
	///     A definition table whose record width is known but whose record contents are not.
	/// </summary>
	/// <remarks>
	///     Four of the twelve layout tables fall into this category. Their widths are known, so they can be stepped over
	///     without losing the chunk walk, and their bytes are kept verbatim so that nothing is lost, but no meaning is
	///     attributed to them. They are rarely populated: most tag groups ship them empty.
	/// </remarks>
	public class FifthGenOpaqueTable
	{
		internal FifthGenOpaqueTable(int magic, int recordSize, int count, byte[] rawData)
		{
			Magic = magic;
			RecordSize = recordSize;
			Count = count;
			RawData = rawData;
		}

		/// <summary>
		///     Gets the table's chunk magic.
		/// </summary>
		public int Magic { get; private set; }

		/// <summary>
		///     Gets the table's chunk magic as a printable string.
		/// </summary>
		public string MagicString
		{
			get { return FifthGenChunk.MagicToString(Magic); }
		}

		/// <summary>
		///     Gets the width of one record in bytes.
		/// </summary>
		public int RecordSize { get; private set; }

		/// <summary>
		///     Gets the number of records in the table.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		///     Gets the table's raw bytes.
		/// </summary>
		public byte[] RawData { get; private set; }

		/// <summary>
		///     Gets the raw bytes of one record.
		/// </summary>
		/// <param name="index">The index of the record.</param>
		/// <returns>The record's bytes.</returns>
		public byte[] GetRecord(int index)
		{
			if (index < 0 || index >= Count)
			{
				throw new System.ArgumentOutOfRangeException(nameof(index),
					$"'{MagicString}' has {Count} record(s); {index} is out of range.");
			}

			var result = new byte[RecordSize];
			System.Array.Copy(RawData, index*RecordSize, result, 0, RecordSize);
			return result;
		}

		public override string ToString()
		{
			return $"{MagicString} ({Count} undecoded record(s) of {RecordSize} byte(s))";
		}
	}
}
