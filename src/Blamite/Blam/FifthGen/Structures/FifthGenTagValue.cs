using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The value of one field of one struct instance in a fifth-generation tag payload.
	/// </summary>
	/// <remarks>
	///     A field's inline bytes are always kept, whatever the field's type, so that nothing is lost to interpretation. The
	///     derived classes add meaning on top of those bytes for the types whose contents the format actually pins down.
	/// </remarks>
	/// <remarks>
	///     <para>
	///         <see cref="RawData" /> is the authority for a field's inline bytes: every mutator on a derived class writes
	///         through to it (re-encoding the same number of bytes it already occupies, since a field's inline width is
	///         fixed by the schema) rather than leaving it to go stale. <see cref="FifthGenTagWriter" /> leans on this - it
	///         never needs per-type encoding knowledge for an inline value, only <see cref="RawData" /> and, for the
	///         handful of types whose real content lives in a nested section instead (a stringID's text, a tag reference's
	///         path, a data field's buffer, a block's elements), the decoded value that section-writing logic already
	///         understands.
	///     </para>
	///     <para>
	///         <see cref="Dirty" /> marks a value that was written through since it was parsed. The writer uses it, together
	///         with <see cref="IsDirtyRecursive" />, to decide whether a span of the original payload can be replayed
	///         verbatim or has to be re-derived from the (possibly edited) decoded model - see <see cref="FifthGenTagWriter" />
	///         for why that distinction matters for more than just efficiency.
	///     </para>
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

		/// <summary>
		///     Gets whether this value was written through since it was parsed.
		/// </summary>
		public bool Dirty { get; private set; }

		/// <summary>
		///     Gets whether this value, or anything nested inside it, was written through since it was parsed.
		/// </summary>
		/// <remarks>
		///     Equal to <see cref="Dirty" /> for a value with nothing nested inside it. Overridden by the container types -
		///     <see cref="FifthGenStructValue" />, <see cref="FifthGenArrayValue" /> and <see cref="FifthGenBlockValue" /> -
		///     which have no meaningful <see cref="Dirty" /> of their own but need to answer for what they hold.
		/// </remarks>
		internal virtual bool IsDirtyRecursive
		{
			get { return Dirty; }
		}

		/// <summary>
		///     Replaces this value's inline bytes and marks it dirty.
		/// </summary>
		/// <param name="newRawData">
		///     The field's new inline bytes. Must be the same length as <see cref="RawData" /> - a field's inline width is
		///     fixed by the schema, so growing or shrinking it here would desynchronize every sibling field's offset within
		///     the struct.
		/// </param>
		protected void ReplaceRawData(byte[] newRawData)
		{
			if (newRawData == null)
				throw new ArgumentNullException(nameof(newRawData));
			if (newRawData.Length != RawData.Length)
			{
				throw new ArgumentException(
					$"Field '{Name}' ({TypeName}) occupies {RawData.Length} inline byte(s); {newRawData.Length} were given. " +
					"A field's inline width is fixed by the schema and cannot change.", nameof(newRawData));
			}

			RawData = newRawData;
			Dirty = true;
		}

		/// <summary>
		///     Marks this value dirty without touching <see cref="RawData" />, for the derived classes whose real content
		///     lives in a nested section rather than inline.
		/// </summary>
		protected void MarkDirty()
		{
			Dirty = true;
		}

		/// <summary>
		///     Encodes a whole number into a fixed-width byte array using the same primitives the rest of Blamite's I/O
		///     layer uses to read and write one, so a mutated field's bytes are indistinguishable from ones the format
		///     itself produced.
		/// </summary>
		/// <param name="value">The value to encode, sign-extended or truncated to <paramref name="width" /> as needed.</param>
		/// <param name="width">The field's inline width in bytes: 1, 2, 4 or 8.</param>
		/// <param name="endianness">The payload's endianness.</param>
		/// <returns>The encoded bytes.</returns>
		internal static byte[] EncodeInteger(long value, int width, Endian endianness)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new EndianWriter(stream, endianness);
				switch (width)
				{
					case 1:
						writer.WriteByte((byte) value);
						break;
					case 2:
						writer.WriteUInt16((ushort) value);
						break;
					case 4:
						writer.WriteUInt32((uint) value);
						break;
					case 8:
						writer.WriteUInt64((ulong) value);
						break;
					default:
						throw new NotSupportedException($"{width} byte integer fields cannot be written.");
				}
				return stream.ToArray();
			}
		}

		/// <summary>
		///     Encodes a 32-bit float the same way <see cref="EncodeInteger" /> encodes a whole number.
		/// </summary>
		/// <param name="value">The value to encode.</param>
		/// <param name="endianness">The payload's endianness.</param>
		/// <returns>The encoded bytes.</returns>
		internal static byte[] EncodeFloat(float value, Endian endianness)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new EndianWriter(stream, endianness);
				writer.WriteFloat(value);
				return stream.ToArray();
			}
		}

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

		/// <summary>
		///     Sets the field's value, re-encoding <see cref="FifthGenTagValue.RawData" /> to the field's existing inline
		///     width.
		/// </summary>
		/// <param name="value">The new value. Truncated or sign-extended to the field's width, as <see cref="IsSigned" /> dictates.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetValue(long value, Endian endianness)
		{
			ReplaceRawData(EncodeInteger(value, RawData.Length, endianness));
			SignedValue = value;
			Value = unchecked((ulong) value);
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

		/// <summary>
		///     Sets the field's value, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="value">The new value.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetValue(float value, Endian endianness)
		{
			ReplaceRawData(EncodeFloat(value, endianness));
			Value = value;
		}

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

		/// <summary>
		///     Sets the field's value, re-encoding <see cref="FifthGenTagValue.RawData" /> as UTF-8 and NUL-padding it to
		///     the field's fixed inline width.
		/// </summary>
		/// <param name="value">
		///     The new value. Its UTF-8 encoding, including at least one padding byte to keep the field NUL-terminated
		///     on disk the way <see cref="Blamite.IO.EndianReader.ReadUTF8(int)" /> expects, must fit within the field's
		///     inline width.
		/// </param>
		/// <exception cref="ArgumentException">Thrown if the encoded value does not fit in the field's inline width.</exception>
		public void SetValue(string value)
		{
			value = value ?? string.Empty;
			byte[] encoded = System.Text.Encoding.UTF8.GetBytes(value);
			if (encoded.Length >= RawData.Length)
			{
				throw new ArgumentException(
					$"Field '{Name}' ({TypeName}) has {RawData.Length} inline byte(s) of room, including a terminator; " +
					$"\"{value}\" needs {encoded.Length + 1}. This is a fixed-width field and cannot grow.", nameof(value));
			}

			var padded = new byte[RawData.Length];
			Array.Copy(encoded, padded, encoded.Length);
			ReplaceRawData(padded);
			Value = value;
		}

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

		/// <summary>
		///     Sets the field's text. The section this produces is written with no NUL terminator, matching most - but, per
		///     <see cref="FifthGenTagDataReader" />, not all - of the string sections found in shipped tags; a reader
		///     tolerant of both forms (as this codebase's own is) reads either back correctly.
		/// </summary>
		/// <param name="value">The new text. <c>null</c> is treated as empty.</param>
		/// <remarks>
		///     This only ever touches the section; the inline <see cref="Id" /> is left exactly as parsed. Nothing in this
		///     codebase has derived what hash (if any) the runtime expects <see cref="Id" /> to agree with the text on for
		///     this generation - CE tags carry their own text, unlike earlier generations' shared stringID tables - so
		///     recomputing it here would be a guess rather than a documented rule. A caller that knows better can still
		///     reach past this and set <see cref="Id" /> directly; nothing currently does.
		/// </remarks>
		public void SetValue(string value)
		{
			Value = value ?? string.Empty;
			MarkDirty();
		}

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
		///     Gets whether the reference points at nothing: an entirely empty (zero byte) section.
		/// </summary>
		/// <remarks>
		///     This is <see cref="GroupMagic" />, not <see cref="Path" /> - a reference can legitimately have an empty
		///     path and still not be null. <see cref="FifthGenTagDataReader.ReadReferenceSection" /> writes a section as
		///     small as four bytes - the group four-CC alone, with no path text following it at all - for a reference that
		///     names a group but not a specific tag; that is a real shape found in shipped tags, not a malformed one, and
		///     it decodes to the same empty <see cref="Path" /> a genuinely null (zero byte) section does. Only a null
		///     section forces <see cref="GroupMagic" /> to zero, which is why checking it rather than the path is the way
		///     to tell the two apart. Getting this wrong here first showed up as <see cref="FifthGenTagWriter" /> silently
		///     dropping a real group magic while re-encoding a reference whose path merely happened to be empty.
		/// </remarks>
		public bool IsNull
		{
			get { return GroupMagic == 0; }
		}

		/// <summary>
		///     Sets the reference, re-encoding both the section this produces and the inline four-CC copy at
		///     <see cref="InlineGroupMagic" /> so the two stay in agreement (the parser only ever warns about that
		///     disagreement, but there is no reason to introduce a fresh one).
		/// </summary>
		/// <param name="groupMagic">
		///     The target's group four-CC, packed the way <see cref="Util.CharConstant.FromString" /> packs it. Zero
		///     produces a null reference (an empty section) regardless of <paramref name="path" /> - see the remarks on
		///     <see cref="IsNull" /> for why a non-zero group with an empty path is a different, and valid, thing.
		/// </param>
		/// <param name="path">The target's tag path. <c>null</c> is treated as empty.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetReference(int groupMagic, string path, Endian endianness)
		{
			GroupMagic = groupMagic;
			Path = (groupMagic == 0) ? string.Empty : (path ?? string.Empty);

			var inline = (byte[]) RawData.Clone();
			byte[] encodedMagic = EncodeInteger(unchecked((uint) groupMagic), 4, endianness);
			Array.Copy(encodedMagic, inline, 4);
			ReplaceRawData(inline);
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

		/// <summary>
		///     Sets the field's buffer. Unlike a fixed-width inline field, this is free to grow or shrink - it lives in its
		///     own <c>tgda</c> section, whose declared size <see cref="FifthGenTagWriter" /> recomputes.
		/// </summary>
		/// <param name="contents">The new buffer. <c>null</c> is treated as empty.</param>
		public void SetContents(byte[] contents)
		{
			Contents = contents ?? new byte[0];
			MarkDirty();
		}

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

		/// <summary>
		///     Sets the resource's contents and attachment.
		/// </summary>
		/// <param name="contents">The new contents. <c>null</c> is treated as empty.</param>
		/// <param name="attached">Whether the resource should be marked attached.</param>
		/// <remarks>
		///     No pageable resource has turned up in any tag sampled while this was written, so the section magics
		///     <see cref="FifthGenTagWriter" /> writes for one are inferred from <see cref="FifthGenFieldTypes.IsPageableResourceSection" />
		///     and <see cref="FifthGenFieldTypes.IsResourceAttached" /> rather than checked against real data.
		/// </remarks>
		public void SetContents(byte[] contents, bool attached)
		{
			Contents = contents ?? new byte[0];
			IsAttached = attached;
			MarkDirty();
		}

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

		/// <summary>
		///     A struct field is never itself set - see the remarks on <see cref="FifthGenTagValue.RawData" /> for why
		///     <see cref="FifthGenTagWriter" /> does not trust this value's own inline bytes for reconstruction - so this
		///     answers purely for what is nested inside <see cref="Value" />.
		/// </summary>
		internal override bool IsDirtyRecursive
		{
			get { return Value.IsDirty; }
		}

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

		internal override bool IsDirtyRecursive
		{
			get
			{
				foreach (FifthGenTagStruct element in Elements)
				{
					if (element.IsDirty)
						return true;
				}
				return false;
			}
		}

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

		internal override bool IsDirtyRecursive
		{
			get { return Value != null && Value.IsDirty; }
		}

		public override string ToString()
		{
			int count = (Value != null) ? Value.Elements.Count : 0;
			return $"{TypeName} '{Name}' = {count} element(s)";
		}
	}
}
