using System.Collections.Generic;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The value of one field of one struct instance in a fifth-generation tag payload.
	/// </summary>
	/// <remarks>
	///     A field's inline bytes are always kept, whatever the field's type, so that nothing is lost to interpretation. The
	///     derived classes add meaning on top of those bytes for the types whose contents the format actually pins down.
	/// </remarks>
	public abstract class FifthGenTagValue
	{
		protected FifthGenTagValue(FifthGenFieldDefinition field, byte[] rawData)
		{
			Field = field;
			RawData = rawData;
		}

		/// <summary>
		///     Gets the definition of the field this value belongs to.
		/// </summary>
		public FifthGenFieldDefinition Field { get; private set; }

		/// <summary>
		///     Gets the field's name.
		/// </summary>
		public string Name
		{
			get { return Field.Name; }
		}

		/// <summary>
		///     Gets the field's type.
		/// </summary>
		public FifthGenFieldType Type
		{
			get { return Field.Type; }
		}

		/// <summary>
		///     Gets the field's type name as the payload declares it.
		/// </summary>
		public string TypeName
		{
			get { return Field.TypeName; }
		}

		/// <summary>
		///     Gets the field's inline bytes, verbatim.
		/// </summary>
		public byte[] RawData { get; private set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}'";
		}
	}

	/// <summary>
	///     A field whose inline bytes carry no meaning this parser attributes to them - padding, a custom field, a composite
	///     real such as a point or a colour, or a type outside the known vocabulary.
	/// </summary>
	/// <remarks>
	///     Composite reals are deliberately here rather than decoded into components. The format states their widths but says
	///     nothing about their internal layout, and a rectangle in eight bytes is four shorts while a point in eight bytes is
	///     two floats. Guessing would be worse than handing back the bytes.
	/// </remarks>
	public class FifthGenOpaqueValue : FifthGenTagValue
	{
		internal FifthGenOpaqueValue(FifthGenFieldDefinition field, byte[] rawData)
			: base(field, rawData)
		{
		}
	}

	/// <summary>
	///     A field whose inline bytes are a single whole number: an integer, an enumeration, a flag word, a block index or a
	///     four-CC.
	/// </summary>
	public class FifthGenIntegerValue : FifthGenTagValue
	{
		internal FifthGenIntegerValue(FifthGenFieldDefinition field, byte[] rawData, ulong value, bool signed,
			long signedValue)
			: base(field, rawData)
		{
			Value = value;
			IsSigned = signed;
			SignedValue = signedValue;
		}

		/// <summary>
		///     Gets the field's value as an unsigned whole number.
		/// </summary>
		public ulong Value { get; private set; }

		/// <summary>
		///     Gets whether the field's type is signed.
		/// </summary>
		public bool IsSigned { get; private set; }

		/// <summary>
		///     Gets the field's value as a signed whole number, sign-extended from the field's width.
		/// </summary>
		public long SignedValue { get; private set; }

		/// <summary>
		///     Gets the name of the enumeration option the value selects, or <c>null</c> if the field is not an enumeration or the
		///     value is out of range.
		/// </summary>
		public string OptionName
		{
			get
			{
				FifthGenEnumDefinition definition = Field.Enum;
				if (definition == null || SignedValue < 0 || SignedValue >= definition.Options.Count)
					return null;
				return definition.Options[(int) SignedValue];
			}
		}

		public override string ToString()
		{
			string option = OptionName;
			string rendered = IsSigned ? SignedValue.ToString() : Value.ToString();
			return (option != null) ? $"{TypeName} '{Name}' = {rendered} ({option})" : $"{TypeName} '{Name}' = {rendered}";
		}
	}

	/// <summary>
	///     A field whose inline bytes are a single 32-bit float.
	/// </summary>
	public class FifthGenRealValue : FifthGenTagValue
	{
		internal FifthGenRealValue(FifthGenFieldDefinition field, byte[] rawData, float value)
			: base(field, rawData)
		{
			Value = value;
		}

		/// <summary>
		///     Gets the field's value.
		/// </summary>
		public float Value { get; private set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Value}";
		}
	}

	/// <summary>
	///     A fixed-width inline string field.
	/// </summary>
	public class FifthGenStringValue : FifthGenTagValue
	{
		internal FifthGenStringValue(FifthGenFieldDefinition field, byte[] rawData, string value)
			: base(field, rawData)
		{
			Value = value;
		}

		/// <summary>
		///     Gets the field's value, with its padding stripped.
		/// </summary>
		public string Value { get; private set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = \"{Value}\"";
		}
	}

	/// <summary>
	///     A stringID field, whose literal text travels with it.
	/// </summary>
	/// <remarks>
	///     There is no global stringID table in a tag payload. Each stringID field's four inline bytes are followed by a section
	///     of its own carrying the text, so a payload resolves its own strings and nothing outside it is needed.
	/// </remarks>
	public class FifthGenStringIDValue : FifthGenTagValue
	{
		internal FifthGenStringIDValue(FifthGenFieldDefinition field, byte[] rawData, StringID id)
			: base(field, rawData)
		{
			Id = id;
		}

		/// <summary>
		///     Gets the field's stringID value.
		/// </summary>
		public StringID Id { get; private set; }

		/// <summary>
		///     Gets the literal text the field's own section carries.
		/// </summary>
		public string Value { get; internal set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = \"{Value}\" (0x{Id.Value:X8})";
		}
	}

	/// <summary>
	///     A tag reference field, whose target path travels with it.
	/// </summary>
	/// <remarks>
	///     As with stringIDs there is no tag name table in a payload. The field's own section carries the reference in full: a
	///     four-CC naming the target's group, stored reversed like every other four-CC in the format, followed by the path. A
	///     section of zero length is a null reference. The interpretation of the sixteen <em>inline</em> bytes as a four-CC, two
	///     words the runtime fills in and a datum index follows the shape a reference has had since the third generation and is
	///     an assumption; the section is the authority, and <see cref="InlineGroupMagic" /> is exposed separately so that the two
	///     can be compared.
	/// </remarks>
	public class FifthGenTagReferenceValue : FifthGenTagValue
	{
		internal FifthGenTagReferenceValue(FifthGenFieldDefinition field, byte[] rawData, int inlineGroupMagic,
			DatumIndex index)
			: base(field, rawData)
		{
			InlineGroupMagic = inlineGroupMagic;
			Index = index;
		}

		/// <summary>
		///     Gets the four-CC of the group the reference points at, as stated by the field's own section. Zero for a null
		///     reference.
		/// </summary>
		public int GroupMagic { get; internal set; }

		/// <summary>
		///     Gets the four-CC in the reference's inline bytes, which is where a third-generation reference keeps it.
		/// </summary>
		public int InlineGroupMagic { get; private set; }

		/// <summary>
		///     Gets the referenced group's four-CC as a printable string.
		/// </summary>
		public string GroupTag
		{
			get { return FifthGenChunk.MagicToString(GroupMagic); }
		}

		/// <summary>
		///     Gets the datum index the reference carries. It is meaningless in a cooked payload, where references resolve by name.
		/// </summary>
		public DatumIndex Index { get; private set; }

		/// <summary>
		///     Gets the tag path the field's own section carries.
		/// </summary>
		public string Path { get; internal set; }

		/// <summary>
		///     Gets whether the reference points at nothing.
		/// </summary>
		public bool IsNull
		{
			get { return string.IsNullOrEmpty(Path); }
		}

		public override string ToString()
		{
			return IsNull ? $"{TypeName} '{Name}' = null" : $"{TypeName} '{Name}' = {Path}.{GroupTag}";
		}
	}

	/// <summary>
	///     A data field, whose contents are a variable-length byte buffer in a section of its own.
	/// </summary>
	public class FifthGenDataValue : FifthGenTagValue
	{
		internal FifthGenDataValue(FifthGenFieldDefinition field, byte[] rawData)
			: base(field, rawData)
		{
			Contents = new byte[0];
		}

		/// <summary>
		///     Gets the buffer the field's own section carries.
		/// </summary>
		public byte[] Contents { get; internal set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Contents.Length} byte(s)";
		}
	}

	/// <summary>
	///     A pageable resource field.
	/// </summary>
	/// <remarks>
	///     The section magic states whether a resource is attached, which is the only thing about the section that is
	///     understood. Its contents are preserved but not interpreted.
	/// </remarks>
	public class FifthGenResourceValue : FifthGenTagValue
	{
		internal FifthGenResourceValue(FifthGenFieldDefinition field, byte[] rawData)
			: base(field, rawData)
		{
			Contents = new byte[0];
		}

		/// <summary>
		///     Gets whether the resource is attached, as reported by the section magic.
		/// </summary>
		public bool IsAttached { get; internal set; }

		/// <summary>
		///     Gets the section's undecoded contents.
		/// </summary>
		public byte[] Contents { get; internal set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Contents.Length} byte(s), {(IsAttached ? "attached" : "detached")}";
		}
	}

	/// <summary>
	///     A struct field, whose sub-struct is inlined into the parent's packed data.
	/// </summary>
	public class FifthGenStructValue : FifthGenTagValue
	{
		internal FifthGenStructValue(FifthGenFieldDefinition field, byte[] rawData, FifthGenTagStruct value)
			: base(field, rawData)
		{
			Value = value;
		}

		/// <summary>
		///     Gets the inlined struct.
		/// </summary>
		public FifthGenTagStruct Value { get; private set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Value}";
		}
	}

	/// <summary>
	///     An inline fixed-length array field.
	/// </summary>
	/// <remarks>
	///     An array emits no section of its own. Its elements are inlined back to back in the parent's packed data and their
	///     sections follow inline where the array's own section would otherwise have been.
	/// </remarks>
	public class FifthGenArrayValue : FifthGenTagValue
	{
		internal FifthGenArrayValue(FifthGenFieldDefinition field, byte[] rawData, FifthGenArrayDefinition definition,
			IList<FifthGenTagStruct> elements)
			: base(field, rawData)
		{
			Definition = definition;
			Elements = elements;
		}

		/// <summary>
		///     Gets the array's definition.
		/// </summary>
		public FifthGenArrayDefinition Definition { get; private set; }

		/// <summary>
		///     Gets the array's elements.
		/// </summary>
		public IList<FifthGenTagStruct> Elements { get; private set; }

		public override string ToString()
		{
			return $"{TypeName} '{Name}'[{Elements.Count}]";
		}
	}

	/// <summary>
	///     A block field, whose elements live in a section of its own.
	/// </summary>
	public class FifthGenBlockValue : FifthGenTagValue
	{
		internal FifthGenBlockValue(FifthGenFieldDefinition field, byte[] rawData, FifthGenBlockDefinition definition)
			: base(field, rawData)
		{
			Definition = definition;
		}

		/// <summary>
		///     Gets the block's definition.
		/// </summary>
		public FifthGenBlockDefinition Definition { get; private set; }

		/// <summary>
		///     Gets the block the field's own section carries.
		/// </summary>
		public FifthGenTagBlock Value { get; internal set; }

		public override string ToString()
		{
			int count = (Value != null) ? Value.Elements.Count : 0;
			return $"{TypeName} '{Name}' = {count} element(s)";
		}
	}
}
