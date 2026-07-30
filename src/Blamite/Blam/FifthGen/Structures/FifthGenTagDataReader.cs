using System;
using System.Collections.Generic;
using System.Text;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     Walks the <c>bdat</c> chunk of a fifth-generation tag payload against the schema the payload declares.
	/// </summary>
	/// <remarks>
	///     <para>
	///         The walk has two halves for every struct instance. First the instance's packed bytes are read - one contiguous
	///         run whose length the schema fixes, with sub-structs and arrays inlined into it. Then the instance's nested
	///         sections are read, in field order, from wherever the cursor now sits. Only some field types contribute a section:
	///         a block, a struct, a stringID, a data buffer, a tag reference and a pageable resource do; an array contributes
	///         none of its own and lets its elements' sections follow inline; everything else is wholly inline.
	///     </para>
	///     <para>
	///         Every nested wrapper is required to be consumed to the byte. A parent advances by a child's declared size, so
	///         bytes left unread inside a nested wrapper never show up in a whole-payload byte count and a walk can appear to
	///         succeed while quietly skipping a section type it has never heard of. Checking each wrapper individually is what
	///         turns that into a precise error naming the chunk and the offset.
	///     </para>
	///     <para>
	///         A struct instance's wrapper can hold more sections than its field list has fields to contribute them - a shipped
	///         scenario writes an empty struct-field wrapper immediately followed by a second, populated one for the same
	///         struct, with no field left to attribute the second wrapper to. Which field the extra section belongs to, and why
	///         the writer duplicated it, is not recoverable from the schema; what is recoverable is that a <c>tgst</c>
	///         section's content is always itself a run of further sections, so leftover bytes after the declared fields are
	///         read are walked the same schema-agnostic way - by their own magics - rather than discarded or rejected. See
	///         <see cref="ReadTrailingSections" />.
	///     </para>
	/// </remarks>
	public class FifthGenTagDataReader
	{
		private static readonly int _blockMagic = CharConstant.FromString("tgbl");
		private static readonly int _structMagic = CharConstant.FromString("tgst");
		private static readonly int _stringIdMagic = CharConstant.FromString("tgsi");
		private static readonly int _dataMagic = CharConstant.FromString("tgda");
		private static readonly int _referenceMagic = CharConstant.FromString("tgrf");

		private readonly FifthGenTagLayout _layout;
		private readonly ICollection<string> _warnings;

		/// <summary>
		///     The chain of blocks, elements and structs the walk is currently inside. A failure deep in a scenario is
		///     unactionable without it.
		/// </summary>
		private readonly List<string> _path = new List<string>();

		/// <summary>
		///     The structs already reported as turning up with an empty wrapper, so that the warning is not repeated for every
		///     element of a block.
		/// </summary>
		private readonly HashSet<int> _elidedStructs = new HashSet<int>();

		/// <summary>
		///     The structs already reported as carrying more sections than their field list accounts for, so that the warning is
		///     not repeated for every element of a block.
		/// </summary>
		private readonly HashSet<int> _trailingStructs = new HashSet<int>();

		/// <summary>
		///     The fields (by <see cref="FifthGenFieldDefinition.Index" />) already reported as decoding to an implausible float
		///     - NaN, infinity or a denormal - so that the warning is not repeated for every element of a block that shares the
		///     same field definition.
		/// </summary>
		private readonly HashSet<int> _implausibleFields = new HashSet<int>();

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagDataReader" /> class.
		/// </summary>
		/// <param name="layout">The schema the payload declared for itself.</param>
		/// <param name="warnings">A collection to record non-fatal problems in. Can be null.</param>
		public FifthGenTagDataReader(FifthGenTagLayout layout, ICollection<string> warnings)
		{
			_layout = layout;
			_warnings = warnings ?? new List<string>();
		}

		/// <summary>
		///     Reads a payload's data.
		/// </summary>
		/// <param name="reader">The stream to read from, with its endianness set to the payload's.</param>
		/// <param name="chunk">The payload's <c>bdat</c> chunk.</param>
		/// <param name="rootStructIndex">The index of the struct the root block's elements are laid out as.</param>
		/// <returns>The root block.</returns>
		public FifthGenTagBlock ReadData(IReader reader, FifthGenChunk chunk, int rootStructIndex)
		{
			reader.SeekTo(chunk.ContentOffset);
			FifthGenChunk root = FifthGenChunk.ReadExpecting(reader, chunk.ContentEnd, _blockMagic, "the payload's root block");
			FifthGenTagBlock result = ReadBlockContents(reader, root, null, _layout.GetStruct(rootStructIndex));
			chunk.EnsureConsumed(reader);
			return result;
		}

		/// <summary>
		///     Reads the contents of a block section: a count, a flags word, the elements' packed data, and then one wrapper
		///     section per element when the flags word is zero.
		/// </summary>
		private FifthGenTagBlock ReadBlockContents(IReader reader, FifthGenChunk chunk, FifthGenBlockDefinition definition,
			FifthGenStructDefinition elementDefinition)
		{
			reader.SeekTo(chunk.ContentOffset);
			uint count = reader.ReadUInt32();
			uint flags = reader.ReadUInt32();

			string label = (definition != null) ? definition.Name : elementDefinition.Name;
			int elementSize = elementDefinition.InlineSize;
			int usable = CheckElementCount(chunk, label, count, flags, elementSize);

			var elements = new List<FifthGenTagStruct>(usable);
			for (var i = 0; i < usable; i++)
				elements.Add(ReadInlineStruct(reader, elementDefinition));

			if (flags == 0)
			{
				for (var i = 0; i < usable; i++)
				{
					FifthGenChunk wrapper = FifthGenChunk.ReadExpecting(reader, chunk.ContentEnd, _structMagic,
						Where($"element {i} of block '{label}'"));
					_path.Add($"{label}[{i}]");
					ReadStructInstanceSections(reader, elements[i], wrapper);
					_path.RemoveAt(_path.Count - 1);
				}
			}

			chunk.EnsureConsumed(reader);
			return new FifthGenTagBlock(definition, elementDefinition, count, flags, elements);
		}

		/// <summary>
		///     Checks that a block's declared element count can fit in the space the section actually has.
		/// </summary>
		/// <returns>The number of elements that will be read.</returns>
		private int CheckElementCount(FifthGenChunk chunk, string label, uint count, uint flags, int elementSize)
		{
			long available = chunk.Size - 8;
			long perElement = elementSize + ((flags == 0) ? FifthGenChunk.HeaderSize : 0);

			if (perElement == 0)
			{
				if (count == 0)
					return 0;

				// An empty element struct with no wrapper section per element means the count is not bounded by
				// anything in the file. Rather than allocate on the strength of a number nothing corroborates, the
				// count is preserved and the elements are not materialized.
				_warnings.Add(
					$"Block '{label}' at 0x{chunk.HeaderOffset:X} declares {count} element(s) of an empty struct and writes no wrapper for them, so nothing bounds the count. The declared count is preserved but no elements were read.");
				return 0;
			}

			if (count*perElement > available)
			{
				throw new FifthGenFormatException(
					$"Block '{label}' at 0x{chunk.HeaderOffset:X} declares {count} element(s) needing at least {count*perElement} byte(s), but its section only has {available}.");
			}

			return (int) count;
		}

		/// <summary>
		///     Reads one struct instance's packed data.
		/// </summary>
		private FifthGenTagStruct ReadInlineStruct(IReader reader, FifthGenStructDefinition definition)
		{
			var values = new List<FifthGenTagValue>(definition.Fields.Count);
			foreach (FifthGenFieldDefinition field in definition.Fields)
				values.Add(ReadInlineValue(reader, field));
			return new FifthGenTagStruct(definition, values);
		}

		private FifthGenTagValue ReadInlineValue(IReader reader, FifthGenFieldDefinition field)
		{
			long start = reader.Position;
			int size = field.InlineSize;
			FifthGenTagValue result = ReadInlineValueContents(reader, field, start, size);

			if (reader.Position != start + size)
			{
				throw new FifthGenFormatException(
					$"Field '{field.Name}' ({field.TypeName}) at 0x{start:X} should occupy {size} inline byte(s) but the read ended at 0x{reader.Position:X}.");
			}

			return result;
		}

		private FifthGenTagValue ReadInlineValueContents(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			switch (field.Type)
			{
				case FifthGenFieldType.Struct:
					FifthGenTagStruct inlined = ReadInlineStruct(reader, field.Struct);
					return new FifthGenStructValue(field, ReadRaw(reader, start, size), inlined);

				case FifthGenFieldType.Array:
					var elements = new List<FifthGenTagStruct>((int) field.Array.Count);
					for (var i = 0; i < field.Array.Count; i++)
						elements.Add(ReadInlineStruct(reader, field.Array.Struct));
					return new FifthGenArrayValue(field, ReadRaw(reader, start, size), field.Array, elements);

				case FifthGenFieldType.StringId:
					uint stringId = reader.ReadUInt32();
					return new FifthGenStringIDValue(field, ReadRaw(reader, start, size), new StringID(stringId));

				case FifthGenFieldType.TagReference:
					return ReadTagReference(reader, field, start, size);

				case FifthGenFieldType.Data:
					return new FifthGenDataValue(field, ReadRaw(reader, start, size));

				case FifthGenFieldType.PageableResource:
					return new FifthGenResourceValue(field, ReadRaw(reader, start, size));

				case FifthGenFieldType.Block:
					return new FifthGenBlockValue(field, ReadRaw(reader, start, size), field.Block);

				case FifthGenFieldType.String:
				case FifthGenFieldType.LongString:
					string text = reader.ReadUTF8(size);
					return new FifthGenStringValue(field, ReadRaw(reader, start, size), text);
			}

			if (FifthGenFieldTypes.IsInteger(field.Type))
				return ReadInteger(reader, field, start, size);

			if (FifthGenFieldTypes.IsReal(field.Type) && size == 4)
			{
				// The typed read has to happen before the raw capture, which leaves the stream at the end of
				// the field.
				float value = reader.ReadFloat();
				return new FifthGenRealValue(field, ReadRaw(reader, start, size), value);
			}

			if (FifthGenFieldTypes.IsVector(field.Type))
				return ReadVector(reader, field, start, size);

			if (FifthGenFieldTypes.IsRealBounds(field.Type))
				return ReadRealBounds(reader, field, start, size);

			if (FifthGenFieldTypes.IsIntegerBounds(field.Type))
				return ReadIntegerBounds(reader, field, start, size);

			if (FifthGenFieldTypes.IsPackedColor(field.Type))
				return ReadPackedColor(reader, field, start, size);

			if (FifthGenFieldTypes.IsRealColor(field.Type))
				return ReadRealColor(reader, field, start, size);

			if (FifthGenFieldTypes.IsRectangle(field.Type))
				return ReadRectangle(reader, field, start, size);

			return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
		}

		/// <summary>
		///     Reads a point, vector, Euler pair/triple, plane or quaternion: two to four floats end to end, with the count
		///     fixed by the field's declared width. See <see cref="FifthGenVectorValue" />'s remarks for why this is the only
		///     reading of those widths that fits every name in <see cref="FifthGenFieldTypes.IsVector" />.
		/// </summary>
		private FifthGenTagValue ReadVector(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			int count = size/4;
			if (size%4 != 0 || count < 2 || count > 4)
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a vector-shaped type of {size} byte(s), which is not a 2-, 3- or 4-float width this parser expects. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			var components = new float[count];
			for (var i = 0; i < count; i++)
				components[i] = reader.ReadFloat();

			CheckFloatPlausibility(field, components);
			return new FifthGenVectorValue(field, ReadRaw(reader, start, size), components);
		}

		/// <summary>
		///     Reads a <c>real bounds</c>, <c>angle bounds</c> or <c>fraction bounds</c> field: two floats read as (low, high).
		/// </summary>
		private FifthGenTagValue ReadRealBounds(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			if (size != 8)
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a real-bounds type of {size} byte(s), not the 8 this parser expects for two floats. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			float lo = reader.ReadFloat();
			float hi = reader.ReadFloat();
			CheckFloatPlausibility(field, new[] {lo, hi});
			return new FifthGenBoundsValue(field, ReadRaw(reader, start, size), lo, hi);
		}

		/// <summary>
		///     Reads a <c>short integer bounds</c> field: two 16-bit integers read as (low, high).
		/// </summary>
		private FifthGenTagValue ReadIntegerBounds(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			if (size != 4)
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a short-integer-bounds type of {size} byte(s), not the 4 this parser expects for two shorts. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			short lo = reader.ReadInt16();
			short hi = reader.ReadInt16();
			return new FifthGenIntegerBoundsValue(field, ReadRaw(reader, start, size), lo, hi);
		}

		/// <summary>
		///     Reads an <c>rgb color</c> or <c>argb color</c> field: a colour packed into a single 32-bit word.
		/// </summary>
		private FifthGenTagValue ReadPackedColor(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			if (size != 4)
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a packed-colour type of {size} byte(s), not the 4 this parser expects for one word. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			uint packed = reader.ReadUInt32();
			return new FifthGenColorValue(field, ReadRaw(reader, start, size), packed, FifthGenFieldTypes.HasAlpha(field.Type));
		}

		/// <summary>
		///     Reads a <c>real rgb color</c> or <c>real argb color</c> field: three or four floats read as colour channels.
		/// </summary>
		private FifthGenTagValue ReadRealColor(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			int count = size/4;
			if (size%4 != 0 || (count != 3 && count != 4))
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a real-colour type of {size} byte(s), not the 12 or 16 this parser expects for three or four floats. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			var components = new float[count];
			for (var i = 0; i < count; i++)
				components[i] = reader.ReadFloat();

			CheckFloatPlausibility(field, components);
			return new FifthGenRealColorValue(field, ReadRaw(reader, start, size), components,
				FifthGenFieldTypes.HasAlpha(field.Type));
		}

		/// <summary>
		///     Reads a <c>rectangle 2d</c> field: four 16-bit integers. See <see cref="FifthGenRectangleValue" />'s remarks for
		///     why this is shorts rather than the two floats the same 8-byte width would give a <see cref="FifthGenVectorValue" />.
		/// </summary>
		private FifthGenTagValue ReadRectangle(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			if (size != 8)
			{
				_warnings.Add(
					$"Field '{field.Name}' ({field.TypeName}) is a rectangle type of {size} byte(s), not the 8 this parser expects for four shorts. Its bytes are preserved.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			var components = new short[4];
			for (var i = 0; i < 4; i++)
				components[i] = reader.ReadInt16();

			return new FifthGenRectangleValue(field, ReadRaw(reader, start, size), components);
		}

		/// <summary>
		///     Checks a composite real field's components for NaN, infinity or a denormal - what garbage looks like once it is
		///     misread as a float - and records a warning the first time a given field definition fails so that one bad field
		///     does not produce one warning per element of a block.
		/// </summary>
		/// <remarks>
		///     This is a plausibility check, not a proof: a wrong-but-finite-and-normal float layout would pass it. It exists to
		///     catch the failure mode that is easy to produce by accident - reading the wrong number of components, or reading
		///     floats over bytes that are not floats at all - which is exactly the mistake a wrong composite layout would make.
		/// </remarks>
		private void CheckFloatPlausibility(FifthGenFieldDefinition field, IReadOnlyList<float> components)
		{
			for (var i = 0; i < components.Count; i++)
			{
				if (IsPlausibleFloat(components[i]))
					continue;

				if (_implausibleFields.Add(field.Index))
				{
					_warnings.Add(
						$"{Where($"field '{field.Name}' ({field.TypeName})")} decoded component {i} of {components.Count} as {components[i]}, which is NaN, infinite or a denormal. The float layout assumed for '{field.TypeName}' may not hold for this field; its bytes are preserved regardless.");
				}
				return;
			}
		}

		/// <summary>
		///     Determines whether a float looks like a value a tag author or the engine would plausibly have written, as
		///     opposed to what turns up when unrelated bytes are misread as a float: NaN, infinity, or a denormal (an exponent
		///     of all zero bits with a non-zero mantissa).
		/// </summary>
		private static bool IsPlausibleFloat(float value)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
				return false;
			if (value == 0f)
				return true;

			int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
			return ((bits >> 23) & 0xFF) != 0;
		}

		private FifthGenTagValue ReadInteger(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			ulong value;
			long signed;
			switch (size)
			{
				case 1:
					value = reader.ReadByte();
					signed = (sbyte) value;
					break;
				case 2:
					value = reader.ReadUInt16();
					signed = (short) value;
					break;
				case 4:
					value = reader.ReadUInt32();
					signed = (int) value;
					break;
				case 8:
					value = reader.ReadUInt64();
					signed = (long) value;
					break;
				default:
					_warnings.Add(
						$"Field '{field.Name}' ({field.TypeName}) is a whole number type of {size} byte(s), which is not a width this parser can decode. Its bytes are preserved.");
					return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			return new FifthGenIntegerValue(field, ReadRaw(reader, start, size), value,
				FifthGenFieldTypes.IsSignedInteger(field.Type), signed);
		}

		private FifthGenTagValue ReadTagReference(IReader reader, FifthGenFieldDefinition field, long start, int size)
		{
			if (size < 16)
			{
				_warnings.Add(
					$"Tag reference '{field.Name}' declares {size} inline byte(s), fewer than the 16 a reference needs. Its bytes are preserved undecoded.");
				return new FifthGenOpaqueValue(field, ReadRaw(reader, start, size));
			}

			int groupMagic = FifthGenChunk.ReadMagic(reader);
			reader.Skip(8);
			var index = new DatumIndex(reader.ReadUInt32());
			return new FifthGenTagReferenceValue(field, ReadRaw(reader, start, size), groupMagic, index);
		}

		/// <summary>
		///     Reads a field's inline bytes verbatim and leaves the stream at the end of them.
		/// </summary>
		private byte[] ReadRaw(IReader reader, long start, int size)
		{
			reader.SeekTo(start);
			return reader.ReadBlock(size);
		}

		/// <summary>
		///     Reads a struct instance's sections out of the wrapper that holds them.
		/// </summary>
		/// <remarks>
		///     A wrapper of zero length holds nothing, and that is not the same thing as a struct with nothing to hold: a struct
		///     whose fields would contribute sections still turns up with an empty wrapper in shipped tags. The empty wrapper
		///     wins, because it is what the file says. The alternative - insisting on the sections the schema implies - is what
		///     makes a scenario fail to walk. This is reported once per struct so that the elision is visible without drowning
		///     the caller; one shipped block writes two thousand empty wrappers in a row.
		/// </remarks>
		private void ReadWrappedSections(IReader reader, FifthGenTagStruct instance, FifthGenChunk wrapper)
		{
			if (wrapper.Size == 0)
			{
				if (instance.Definition.EmitsSections && _elidedStructs.Add(instance.Definition.Index))
				{
					_warnings.Add(
						$"{Where($"struct '{instance.Definition.Name}'")} has an empty wrapper at 0x{wrapper.HeaderOffset:X} even though its fields would contribute nested sections. The wrapper was taken at its word and the sections treated as absent.");
				}
				return;
			}

			ReadStructInstanceSections(reader, instance, wrapper);
		}

		/// <summary>
		///     Reads a struct instance's wrapper in full: its fields' sections against the schema, then whatever the wrapper still
		///     holds beyond them, then asserts the wrapper was consumed exactly.
		/// </summary>
		/// <remarks>
		///     This is the one place the strict-consumption assertion is applied to a struct's own wrapper, so both the schema
		///     walk and the fallback that covers what it misses run before it, and the assertion still has the last word: if the
		///     fallback cannot account for what is left either, the wrapper is exactly as unconsumed as it would have been
		///     without it, and the same error fires.
		/// </remarks>
		private void ReadStructInstanceSections(IReader reader, FifthGenTagStruct instance, FifthGenChunk wrapper)
		{
			ReadSections(reader, instance, wrapper);
			ReadTrailingSections(reader, instance, wrapper);
			wrapper.EnsureConsumed(reader);
		}

		/// <summary>
		///     Reads whatever a struct instance's wrapper still holds after its declared fields' sections have been read.
		/// </summary>
		/// <remarks>
		///     <para>
		///         A shipped scenario's <c>scenario_effect_scenery_block</c> writes an empty <c>struct</c>-field wrapper for
		///         <c>multiplayer data</c> immediately followed by a second, fully populated wrapper of the same shape - two
		///         <c>tgst</c> sections where the field list has one field left to read a section for. Nothing in the schema
		///         names a second field to attribute it to, in this struct or any other, so it cannot be modelled as a field's
		///         value the way every other section in this reader is.
		///     </para>
		///     <para>
		///         What can be shown from the bytes themselves: a <c>tgst</c> section's content is never anything other than a
		///         run of further sections - that is the whole of what a struct's wrapper is for - so it can be walked by magic
		///         alone, with no struct definition to check field-by-field against, the same way the outermost schema-driven
		///         walk would if it had one. Every other section shape declares its own length and needs no schema either. A
		///         <c>tgbl</c> is the one shape this cannot cover: a block's bounds depend on an element struct and a count this
		///         fallback has no way to learn, so one turning up here remains a hard failure rather than a guess.
		///     </para>
		/// </remarks>
		private void ReadTrailingSections(IReader reader, FifthGenTagStruct instance, FifthGenChunk wrapper)
		{
			if (reader.Position >= wrapper.ContentEnd)
				return;

			var extra = new List<FifthGenTrailingSection>();
			while (reader.Position < wrapper.ContentEnd)
				extra.Add(ReadGenericSection(reader, wrapper.ContentEnd));
			instance.TrailingSections = extra;

			if (_trailingStructs.Add(instance.Definition.Index))
			{
				_warnings.Add(
					$"{Where($"struct '{instance.Definition.Name}'")} holds {extra.Count} more section(s) at 0x{extra[0].HeaderOffset:X} beyond what its field list accounts for. They were preserved without being attributed to a field; see FifthGenTrailingSection.");
			}
		}

		/// <summary>
		///     Reads one section by its magic alone, with no field or struct definition to check it against.
		/// </summary>
		private FifthGenTrailingSection ReadGenericSection(IReader reader, long limit)
		{
			FifthGenChunk section = FifthGenChunk.Read(reader, limit, Where("an unattributed trailing section"));

			if (section.Magic == _structMagic)
			{
				var children = new List<FifthGenTrailingSection>();
				while (reader.Position < section.ContentEnd)
					children.Add(ReadGenericSection(reader, section.ContentEnd));
				section.EnsureConsumed(reader);
				return new FifthGenTrailingSection(section.Magic, section.HeaderOffset, null, children);
			}

			if (section.Magic == _stringIdMagic || section.Magic == _dataMagic || section.Magic == _referenceMagic ||
				FifthGenFieldTypes.IsPageableResourceSection(section.Magic))
			{
				return new FifthGenTrailingSection(section.Magic, section.HeaderOffset, section.ReadContent(reader), null);
			}

			throw new FifthGenFormatException(
				$"{Where("an unattributed trailing section")} at 0x{section.HeaderOffset:X} has magic '{section.MagicString}', which cannot be bounded without a field to match it against.");
		}

		/// <summary>
		///     Reads the nested sections a struct instance's fields contribute, in field order.
		/// </summary>
		private void ReadSections(IReader reader, FifthGenTagStruct instance, FifthGenChunk parent)
		{
			foreach (FifthGenTagValue value in instance.Values)
			{
				if (value is FifthGenBlockValue block)
				{
					FifthGenChunk section = ReadSection(reader, parent, _blockMagic, value);
					FifthGenStructDefinition elementDefinition = (block.Definition != null)
						? block.Definition.Struct
						: _layout.GetStruct((int) value.Field.Aux);
					block.Value = ReadBlockContents(reader, section, block.Definition, elementDefinition);
				}
				else if (value is FifthGenStructValue structure)
				{
					// A struct always writes a wrapper, even when it has nothing to put in it.
					FifthGenChunk section = ReadSection(reader, parent, _structMagic, value);
					_path.Add(value.Name);
					ReadWrappedSections(reader, structure.Value, section);
					_path.RemoveAt(_path.Count - 1);
				}
				else if (value is FifthGenArrayValue array)
				{
					// An array has no wrapper of its own; its elements' sections follow inline.
					for (var i = 0; i < array.Elements.Count; i++)
					{
						_path.Add($"{value.Name}[{i}]");
						ReadSections(reader, array.Elements[i], parent);
						_path.RemoveAt(_path.Count - 1);
					}
				}
				else if (value is FifthGenStringIDValue stringId)
				{
					FifthGenChunk section = ReadSection(reader, parent, _stringIdMagic, value);
					stringId.Value = ReadSectionString(reader, section);
				}
				else if (value is FifthGenTagReferenceValue reference)
				{
					FifthGenChunk section = ReadSection(reader, parent, _referenceMagic, value);
					ReadReferenceSection(reader, section, reference);
				}
				else if (value is FifthGenDataValue data)
				{
					FifthGenChunk section = ReadSection(reader, parent, _dataMagic, value);
					data.Contents = section.ReadContent(reader);
				}
				else if (value is FifthGenResourceValue resource)
				{
					ReadResourceSection(reader, parent, resource);
				}
			}
		}

		private FifthGenChunk ReadSection(IReader reader, FifthGenChunk parent, int expectedMagic, FifthGenTagValue value)
		{
			return FifthGenChunk.ReadExpecting(reader, parent.ContentEnd, expectedMagic,
				Where($"field '{value.Name}' ({value.TypeName})"));
		}

		/// <summary>
		///     Describes where in the tag the walk currently is.
		/// </summary>
		private string Where(string leaf)
		{
			return (_path.Count == 0) ? leaf : string.Join(" > ", _path) + " > " + leaf;
		}

		private void ReadResourceSection(IReader reader, FifthGenChunk parent, FifthGenResourceValue resource)
		{
			FifthGenChunk section = FifthGenChunk.Read(reader, parent.ContentEnd);
			if (!FifthGenFieldTypes.IsPageableResourceSection(section.Magic))
			{
				throw new FifthGenFormatException(
					$"Expected a pageable resource section for field '{resource.Name}' at 0x{section.HeaderOffset:X} but found '{section.MagicString}'.");
			}

			resource.IsAttached = FifthGenFieldTypes.IsResourceAttached(section.Magic);
			resource.Contents = section.ReadContent(reader);
			if (section.Size > 0)
			{
				_warnings.Add(
					$"Pageable resource '{resource.Name}' at 0x{section.HeaderOffset:X} carries {section.Size} byte(s) whose contents are undecoded. They were preserved verbatim.");
			}
		}

		/// <summary>
		///     Reads a section whose entire content is a string.
		/// </summary>
		private static string ReadSectionString(IReader reader, FifthGenChunk section)
		{
			return DecodeString(section.ReadContent(reader), 0);
		}

		/// <summary>
		///     Reads a tag reference's own section: a four-CC naming the target's group, then the target's path.
		/// </summary>
		/// <remarks>
		///     An empty section is a null reference, which is by far the most common case. A reference pointing at a tag that was
		///     never cooked is ordinary rather than exceptional, and whether a path resolves cannot be answered from one payload
		///     anyway - it takes the whole container set - so nothing here is ever a reason to fail. Only a section that is too
		///     short to hold what it claims, or an inline four-CC that contradicts the section's, is worth a warning.
		/// </remarks>
		private void ReadReferenceSection(IReader reader, FifthGenChunk section, FifthGenTagReferenceValue reference)
		{
			if (section.Size == 0)
			{
				reference.GroupMagic = 0;
				reference.Path = string.Empty;
				return;
			}

			if (section.Size < 4)
			{
				_warnings.Add(
					$"Tag reference '{reference.Name}' at 0x{section.HeaderOffset:X} has a {section.Size} byte section, too short to name a group. It was left unresolved.");
				section.ReadContent(reader);
				reference.Path = string.Empty;
				return;
			}

			reader.SeekTo(section.ContentOffset);
			reference.GroupMagic = FifthGenChunk.ReadMagic(reader);
			reference.Path = DecodeString(reader.ReadBlock((int) section.Size - 4), 0);

			if (reference.InlineGroupMagic != 0 && reference.InlineGroupMagic != reference.GroupMagic)
			{
				_warnings.Add(
					$"Tag reference '{reference.Name}' at 0x{section.HeaderOffset:X} names group '{reference.GroupTag}' in its section but '{FifthGenChunk.MagicToString(reference.InlineGroupMagic)}' inline.");
			}
		}

		/// <summary>
		///     Decodes UTF-8 text that may or may not be NUL-terminated. Both spellings occur.
		/// </summary>
		private static string DecodeString(byte[] raw, int offset)
		{
			int length = raw.Length;
			while (length > offset && raw[length - 1] == 0)
				length--;
			return Encoding.UTF8.GetString(raw, offset, length - offset);
		}
	}
}
