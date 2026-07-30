using System;
using System.Collections.Generic;
using System.Linq;
using Blamite.Serialization;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     One instance of a struct in a fifth-generation tag payload's data.
	/// </summary>
	public class FifthGenTagStruct
	{
		private readonly List<FifthGenTagValue> _values;
		private List<FifthGenTrailingSection> _trailingSections;

		internal FifthGenTagStruct(FifthGenStructDefinition definition, List<FifthGenTagValue> values)
		{
			Definition = definition;
			_values = values;
		}

		/// <summary>
		///     Builds a struct instance with every field at a zeroed or empty default, for use as a freshly appended block
		///     or array element that never came from a parse.
		/// </summary>
		/// <param name="definition">The struct definition to build an instance of.</param>
		/// <returns>A new instance, none of whose fields are individually marked dirty.</returns>
		/// <remarks>
		///     An instance built this way has no counterpart anywhere in a payload's original bytes, so
		///     <see cref="FifthGenTagWriter" /> can never replay bytes for it - it always has to be freshly encoded. That is
		///     tracked at the block level (see <see cref="FifthGenTagBlock.ElementsDirty" />), not per field here, which is
		///     why the fields this returns are left clean: nothing here needs its own <see cref="FifthGenTagValue.Dirty" />
		///     to be true for the writer to do the right thing with it.
		/// </remarks>
		public static FifthGenTagStruct CreateDefault(FifthGenStructDefinition definition)
		{
			var values = new List<FifthGenTagValue>(definition.Fields.Count);
			foreach (FifthGenFieldDefinition field in definition.Fields)
				values.Add(CreateDefaultValue(field));
			return new FifthGenTagStruct(definition, values);
		}

		private static FifthGenTagValue CreateDefaultValue(FifthGenFieldDefinition field)
		{
			var zero = new byte[field.InlineSize];
			switch (field.Type)
			{
				case FifthGenFieldType.Struct:
					return new FifthGenStructValue(field, zero, CreateDefault(field.Struct));

				case FifthGenFieldType.Array:
					var elements = new List<FifthGenTagStruct>((int) field.Array.Count);
					for (var i = 0; i < field.Array.Count; i++)
						elements.Add(CreateDefault(field.Array.Struct));
					return new FifthGenArrayValue(field, zero, field.Array, elements);

				case FifthGenFieldType.StringId:
					return new FifthGenStringIDValue(field, zero, new StringID(0)) {Value = string.Empty};

				case FifthGenFieldType.TagReference:
					return new FifthGenTagReferenceValue(field, zero, 0, DatumIndex.Null) {GroupMagic = 0, Path = string.Empty};

				case FifthGenFieldType.Data:
					return new FifthGenDataValue(field, zero);

				case FifthGenFieldType.PageableResource:
					return new FifthGenResourceValue(field, zero);

				case FifthGenFieldType.Block:
					return new FifthGenBlockValue(field, zero, field.Block)
					{
						Value = new FifthGenTagBlock(field.Block, field.Block?.Struct, 0, 0, new List<FifthGenTagStruct>())
					};

				case FifthGenFieldType.String:
				case FifthGenFieldType.LongString:
					return new FifthGenStringValue(field, zero, string.Empty);
			}

			if (FifthGenFieldTypes.IsInteger(field.Type))
				return new FifthGenIntegerValue(field, zero, 0, FifthGenFieldTypes.IsSignedInteger(field.Type), 0);

			if (FifthGenFieldTypes.IsReal(field.Type) && field.InlineSize == 4)
				return new FifthGenRealValue(field, zero, 0f);

			return new FifthGenOpaqueValue(field, zero);
		}

		/// <summary>
		///     Gets the definition this instance was read against.
		/// </summary>
		public FifthGenStructDefinition Definition { get; private set; }

		/// <summary>
		///     Gets the struct's name.
		/// </summary>
		public string Name
		{
			get { return Definition.Name; }
		}

		/// <summary>
		///     Gets the instance's field values, in declaration order. Padding fields are included so that the list lines up with
		///     the definition's field list.
		/// </summary>
		public IList<FifthGenTagValue> Values
		{
			get { return _values; }
		}

		/// <summary>
		///     Gets whether any field of this instance, or anything nested inside one, was written through since it was
		///     parsed (or, for an instance built by <see cref="CreateDefault" />, since it was built).
		/// </summary>
		/// <remarks>
		///     <see cref="TrailingSections" /> never contributes here - nothing in this codebase mutates them, so they are
		///     always replayed verbatim by <see cref="FifthGenTagWriter" /> regardless of what else in the instance changed.
		/// </remarks>
		internal bool IsDirty
		{
			get { return _values.Any(v => v.IsDirtyRecursive); }
		}

		/// <summary>
		///     Gets the sections found in this instance's wrapper beyond what its declared fields account for, if any.
		/// </summary>
		/// <remarks>
		///     Some shipped structs write more nested sections than their own field list has fields to contribute them: the
		///     wrapper's declared size is larger than the schema-driven walk consumes, and the leftover bytes are themselves more
		///     well-formed sections rather than noise. Which field they belong to, and why the writer emitted them, is not known -
		///     see <see cref="FifthGenTagDataReader" />'s remarks - so they are preserved here rather than attributed to a name.
		///     Empty for the overwhelming majority of instances.
		/// </remarks>
		public IList<FifthGenTrailingSection> TrailingSections
		{
			get { return _trailingSections ?? (_trailingSections = new List<FifthGenTrailingSection>()); }
			internal set { _trailingSections = new List<FifthGenTrailingSection>(value); }
		}

		/// <summary>
		///     Gets the first field with a given name.
		/// </summary>
		/// <param name="name">The name of the field to find.</param>
		/// <returns>The field's value, or <c>null</c> if the struct has no such field.</returns>
		public FifthGenTagValue this[string name]
		{
			get { return _values.FirstOrDefault(v => v.Name == name); }
		}

		/// <summary>
		///     Projects the instance into a <see cref="StructureValueCollection" /> so that code which already works in terms of
		///     Blamite's serialization primitives can consume it.
		/// </summary>
		/// <returns>The instance's values, keyed by field name.</returns>
		/// <remarks>
		///     This is a convenience view, not the authoritative model. A tag definition may give two fields the same name - every
		///     unnamed padding field shares the empty name, for instance - and a keyed collection can only hold one of them, so
		///     anything that needs to be exact should walk <see cref="Values" /> instead. Composite types with no stated internal
		///     layout are carried across as raw bytes.
		/// </remarks>
		public StructureValueCollection ToValueCollection()
		{
			var result = new StructureValueCollection();
			foreach (FifthGenTagValue value in _values)
			{
				if (string.IsNullOrEmpty(value.Name))
					continue;

				if (value is FifthGenIntegerValue integer)
					result.SetInteger(value.Name, integer.Value);
				else if (value is FifthGenRealValue real)
					result.SetFloat(value.Name, real.Value);
				else if (value is FifthGenStringValue text)
					result.SetString(value.Name, text.Value);
				else if (value is FifthGenStringIDValue stringId)
					result.SetStringID(value.Name, stringId.Value);
				else if (value is FifthGenTagReferenceValue reference)
					result.SetString(value.Name, reference.Path);
				else if (value is FifthGenDataValue data)
					result.SetRaw(value.Name, data.Contents);
				else if (value is FifthGenResourceValue resource)
					result.SetRaw(value.Name, resource.Contents);
				else if (value is FifthGenStructValue structure)
					result.SetStruct(value.Name, structure.Value.ToValueCollection());
				else if (value is FifthGenArrayValue array)
					result.SetArray(value.Name, array.Elements.Select(e => e.ToValueCollection()).ToArray());
				else if (value is FifthGenBlockValue block)
					result.SetTagBlock(value.Name, GetBlockCollections(block));
				else
					result.SetRaw(value.Name, value.RawData);
			}
			return result;
		}

		public override string ToString()
		{
			return $"{Name} ({_values.Count} value(s))";
		}

		private static StructureValueCollection[] GetBlockCollections(FifthGenBlockValue block)
		{
			if (block.Value == null)
				return new StructureValueCollection[0];
			return block.Value.Elements.Select(e => e.ToValueCollection()).ToArray();
		}
	}

	/// <summary>
	///     A block of struct instances in a fifth-generation tag payload's data.
	/// </summary>
	public class FifthGenTagBlock
	{
		private readonly List<FifthGenTagStruct> _elements;

		internal FifthGenTagBlock(FifthGenBlockDefinition definition, FifthGenStructDefinition elementDefinition, uint count,
			uint flags, List<FifthGenTagStruct> elements)
		{
			Definition = definition;
			ElementDefinition = elementDefinition;
			DeclaredCount = count;
			Flags = flags;
			_elements = elements;
		}

		/// <summary>
		///     Gets the block's definition, or <c>null</c> for the payload's root block, which no field declares.
		/// </summary>
		public FifthGenBlockDefinition Definition { get; private set; }

		/// <summary>
		///     Gets the definition one element of the block is laid out as.
		/// </summary>
		public FifthGenStructDefinition ElementDefinition { get; private set; }

		/// <summary>
		///     Gets the block's name, or <c>null</c> for the root block.
		/// </summary>
		public string Name
		{
			get { return (Definition != null) ? Definition.Name : null; }
		}

		/// <summary>
		///     Gets the element count the block's section declares.
		/// </summary>
		public uint DeclaredCount { get; private set; }

		/// <summary>
		///     Gets the block's flags word.
		/// </summary>
		/// <remarks>
		///     Nothing in the schema predicts this word, and it changes the shape of the data: when it is zero the block writes a
		///     wrapper section per element, and when it is not, it writes none at all. One shipped block writes two thousand
		///     wrappers the field list gives no hint of, so it has to be read rather than inferred.
		/// </remarks>
		public uint Flags { get; private set; }

		/// <summary>
		///     Gets whether the block writes a wrapper section for each of its elements.
		/// </summary>
		public bool HasElementSections
		{
			get { return Flags == 0; }
		}

		/// <summary>
		///     Gets the block's elements.
		/// </summary>
		public IList<FifthGenTagStruct> Elements
		{
			get { return _elements; }
		}

		/// <summary>
		///     Gets whether elements have been added to, removed from, or reordered within this block since it was parsed.
		/// </summary>
		/// <remarks>
		///     Tracked separately from an individual element's own dirtiness because it means something different to
		///     <see cref="FifthGenTagWriter" />: once the element list itself no longer lines up with the payload it was
		///     parsed from, no element's original bytes can be trusted as that element's bytes any more - not even an
		///     element nobody touched, since everything after an insertion or removal has shifted. The writer falls back to
		///     freshly encoding every element rather than trying to figure out which ones still line up.
		/// </remarks>
		public bool ElementsDirty { get; private set; }

		/// <summary>
		///     Gets whether this block's element list was reshaped, or any element in it (recursively) was written through,
		///     since it was parsed.
		/// </summary>
		internal bool IsDirty
		{
			get { return ElementsDirty || _elements.Any(e => e.IsDirty); }
		}

		/// <summary>
		///     Appends an element to the block.
		/// </summary>
		/// <param name="element">
		///     The element to append, laid out as <see cref="ElementDefinition" /> - typically built with
		///     <see cref="FifthGenTagStruct.CreateDefault" />.
		/// </param>
		public void AddElement(FifthGenTagStruct element)
		{
			InsertElement(_elements.Count, element);
		}

		/// <summary>
		///     Inserts an element into the block at a given position.
		/// </summary>
		/// <param name="index">The position to insert at, from 0 to <see cref="Elements" />'s current count inclusive.</param>
		/// <param name="element">The element to insert, laid out as <see cref="ElementDefinition" />.</param>
		public void InsertElement(int index, FifthGenTagStruct element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));
			_elements.Insert(index, element);
			DeclaredCount = (uint) _elements.Count;
			ElementsDirty = true;
		}

		/// <summary>
		///     Removes the element at a given position.
		/// </summary>
		/// <param name="index">The position of the element to remove.</param>
		public void RemoveElementAt(int index)
		{
			_elements.RemoveAt(index);
			DeclaredCount = (uint) _elements.Count;
			ElementsDirty = true;
		}

		public override string ToString()
		{
			return $"{Name ?? ElementDefinition.Name} ({_elements.Count} element(s), flags 0x{Flags:X8})";
		}
	}

	/// <summary>
	///     A section found trailing a struct instance's wrapper beyond what its declared fields account for.
	/// </summary>
	/// <remarks>
	///     A <c>tgst</c> section's own content is always itself a sequence of further sections, so a trailing <c>tgst</c> is
	///     descended the same schema-agnostic way and its own trailing content becomes <see cref="Children" />. Every other
	///     section shape - <c>tgsi</c>, <c>tgda</c>, <c>tgrf</c>, a pageable resource - declares its own byte length and needs
	///     no schema to bound, so its bytes are kept whole in <see cref="Content" />. See
	///     <see cref="FifthGenTagDataReader" />'s remarks for how this is found and why it is read this way rather than
	///     rejected.
	/// </remarks>
	public class FifthGenTrailingSection
	{
		internal FifthGenTrailingSection(int magic, long headerOffset, byte[] content, IList<FifthGenTrailingSection> children)
		{
			Magic = magic;
			HeaderOffset = headerOffset;
			Content = content;
			Children = children;
		}

		/// <summary>
		///     Gets the section's four-CC magic number.
		/// </summary>
		public int Magic { get; private set; }

		/// <summary>
		///     Gets the section's four-CC magic number as a printable string.
		/// </summary>
		public string MagicString
		{
			get { return FifthGenChunk.MagicToString(Magic); }
		}

		/// <summary>
		///     Gets the offset of the section's header.
		/// </summary>
		public long HeaderOffset { get; private set; }

		/// <summary>
		///     Gets the section's raw content, for a section shape that carries no further sections of its own. Null for a
		///     <c>tgst</c> section, whose content is <see cref="Children" /> instead.
		/// </summary>
		public byte[] Content { get; private set; }

		/// <summary>
		///     Gets the sections nested inside this one, for a <c>tgst</c> section. Null for every other section shape.
		/// </summary>
		public IList<FifthGenTrailingSection> Children { get; private set; }

		public override string ToString()
		{
			return (Children != null)
				? $"{MagicString} ({Children.Count} child section(s))"
				: $"{MagicString} ({Content.Length} byte(s))";
		}
	}
}
