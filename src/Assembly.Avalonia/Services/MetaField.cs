using System;
using System.Collections.Generic;

namespace Assembly.Avalonia.Services
{
	/// <summary>The kinds of field a tag-definition ("plugin") XML can declare.</summary>
	public enum MetaFieldKind
	{
		Comment,
		UInt8, Int8, UInt16, Int16, UInt32, Int32, UInt64, Int64,
		Float32, Undefined, Datum,
		Point2, Point3, Vector2, Vector3, Vector4,
		Degree, Degree2, Degree3,
		Plane2, Plane3,
		Rect16, Quat16, Point16,
		StringId, OldStringId,
		TagReference, DataReference,
		RangeInt16, RangeFloat32, RangeDegree,
		RawData, Ascii, Utf16, HexString,
		ColorInt, ColorF,
		Flags, Enum,
		TagBlock, TagBlockEnd,
		Shader, UnicList,

		/// <summary>
		///     A scalar field of a fifth-generation (Campaign Evolved) tag whose type Blamite
		///     decodes but exposes no write-back API for: every composite "real" shape (a point,
		///     vector, Euler pair/triple, plane, quaternion, bounds pair, packed or float colour,
		///     rectangle - see <c>FifthGenCompositeValue.cs</c>), a variable-length data or
		///     pageable-resource buffer, and anything the payload's own type table does not name at
		///     all. <see cref="TagDocumentViewModel.GetFifthGenScalarDef" /> maps every fifth-generation
		///     field whose type Blamite <em>can</em> write - an integer, a float, an enumeration, a
		///     flag word, a fixed-width inline string - onto the matching classic
		///     <see cref="MetaFieldKind" /> instead, so it gets that kind's real editor for free; this
		///     value is what is left over once that mapping runs out of API to lean on. See
		///     <see cref="MetaFieldDef.NotEditableReason" /> for why, field by field.
		/// </summary>
		FifthGenValue,

		/// <summary>
		///     A fifth-generation stringID field. Unlike a classic <see cref="StringId" /> - an
		///     index into a cache-wide table, so only an existing string can ever be picked - a
		///     Campaign Evolved stringID carries its own literal text in a section of its own (see
		///     <c>FifthGenStringIDValue</c>'s remarks), so it can be freely retyped. Kept as its own
		///     kind rather than folded into <see cref="StringId" /> because the two are not
		///     interchangeable: an editor built for one would either wrongly restrict the other to
		///     an existing table, or wrongly let a classic field grow one that cannot.
		/// </summary>
		FifthGenStringId,

		/// <summary>
		///     A fifth-generation tag-reference field: a target group four-CC plus a path, both
		///     travelling with the field itself (see <c>FifthGenTagReferenceValue</c>'s remarks)
		///     rather than resolved through a cache's datum-index table the way classic
		///     <see cref="TagReference" /> is. Retargeting is <c>FifthGenTagReferenceValue.SetReference</c>,
		///     which has no classic equivalent this codebase's write path drives yet - see
		///     <c>ReadOnlyEditor.ReasonFor</c>'s <see cref="TagReference" /> case.
		/// </summary>
		FifthGenTagReference
	}

	/// <summary>
	///     A single field declared by a tag definition, flattened out of the visitor
	///     callbacks. Offsets are relative to the start of the enclosing structure (the tag's
	///     meta at depth 0, or the current tag block element at depth &gt; 0).
	/// </summary>
	public sealed class MetaFieldDef
	{
		public MetaFieldKind Kind { get; init; }
		public string Name { get; init; } = "";
		public uint Offset { get; init; }
		public int Size { get; init; }
		public int Depth { get; init; }
		public string? Tooltip { get; init; }
		public string? Note { get; init; }

		/// <summary>
		///     When set, wins over the default "TYPE" column text (see <c>MetaRowViewModel.KindLabel</c>).
		///     Used by the fifth-generation projection to show the tag's own declared type name
		///     (e.g. "real point 3d", "long enum") or a container shape ("block", "array[4]",
		///     "struct") instead of the classic plugin-derived label. Null for every plugin-XML
		///     field, so classic rendering is unchanged.
		/// </summary>
		public string? KindLabelOverride { get; init; }

		/// <summary>Bit names for flags, or option names for enums.</summary>
		public List<(string Name, long Value)>? Choices { get; init; }

		/// <summary>
		///     For a fixed-width inline string field ("string"/"long string" in a Campaign Evolved
		///     tag's own type vocabulary, mapped onto <see cref="MetaFieldKind.Ascii" /> - see
		///     <see cref="TagDocumentViewModel.GetFifthGenScalarDef" />): whether <see cref="Size" />
		///     is a UTF-8 byte budget, one byte of which is always reserved for the terminator
		///     <c>FifthGenStringValue.SetValue</c> requires, rather than a one-byte-per-character
		///     Latin-1 budget the way a classic plugin's <c>ascii</c> field is. False for every
		///     classic field, so <c>TextFieldEditor</c>'s existing char-counting behaviour is
		///     unchanged for them.
		/// </summary>
		public bool Utf8Budget { get; init; }

		/// <summary>
		///     A precise, field-specific reason <see cref="IsEditable" /> is false, when the generic
		///     per-<see cref="Kind" /> text <c>ReadOnlyEditor.ReasonFor</c> falls back to would not
		///     say anything true about <em>this</em> field. Set by
		///     <see cref="TagDocumentViewModel.GetFifthGenScalarDef" /> for a Campaign Evolved field
		///     whose type decodes today but has no write-back API in Blamite yet (see
		///     <see cref="MetaFieldKind.FifthGenValue" />'s remarks) - naming the concrete value type
		///     that is missing a setter, not just "this isn't editable". Null everywhere else.
		/// </summary>
		public string? NotEditableReason { get; init; }

		/// <summary>For <see cref="MetaFieldKind.TagBlock" />: the byte size of one element.</summary>
		public uint EntrySize { get; init; }

		/// <summary>
		///     For <see cref="MetaFieldKind.TagBlock" />: index into the flat schema list of the
		///     first child field (depth+1), and the count of immediate+nested descendant defs up
		///     to and including the matching TagBlockEnd. Set after the full schema is parsed
		///     (see PluginSchemaVisitor.LinkBlockRanges).
		/// </summary>
		public int ChildStart { get; internal set; } = -1;
		public int ChildEnd { get; internal set; } = -1;

		public string KindLabel => Kind.ToString();

		public bool IsBlock => Kind == MetaFieldKind.TagBlock;

		/// <summary>Whether this field kind currently has a real (non-mock) editor with write-back.</summary>
		public bool IsEditable => Kind switch
		{
			MetaFieldKind.UInt8 or MetaFieldKind.Int8 or MetaFieldKind.UInt16 or MetaFieldKind.Int16 or
			MetaFieldKind.UInt32 or MetaFieldKind.Int32 or MetaFieldKind.UInt64 or MetaFieldKind.Int64 or
			MetaFieldKind.Float32 or MetaFieldKind.Degree or
			MetaFieldKind.Point2 or MetaFieldKind.Point3 or MetaFieldKind.Vector2 or MetaFieldKind.Vector3 or
			MetaFieldKind.Vector4 or MetaFieldKind.Degree2 or MetaFieldKind.Degree3 or
			MetaFieldKind.RangeInt16 or MetaFieldKind.RangeFloat32 or MetaFieldKind.RangeDegree or
			MetaFieldKind.Enum or MetaFieldKind.Flags or
			MetaFieldKind.ColorInt or
			MetaFieldKind.Ascii or MetaFieldKind.Utf16 or
			MetaFieldKind.StringId or MetaFieldKind.OldStringId or
			MetaFieldKind.FifthGenStringId or MetaFieldKind.FifthGenTagReference => true,
			_ => false
		};

		/// <summary>For an integer <see cref="Kind" />, the field's declared bit width (8/16/32/64).
		/// Zero for anything else. Used to validate a typed value against the width the plugin
		/// actually declared instead of the 64 bits <see cref="FieldEditState.Int" /> happens to be
		/// stored in - see the integer editor's remarks on why silently truncating an overflow is
		/// not acceptable here.</summary>
		public int IntegerBits => Kind switch
		{
			MetaFieldKind.UInt8 or MetaFieldKind.Int8 => 8,
			MetaFieldKind.UInt16 or MetaFieldKind.Int16 => 16,
			MetaFieldKind.UInt32 or MetaFieldKind.Int32 => 32,
			MetaFieldKind.UInt64 or MetaFieldKind.Int64 => 64,
			_ => 0
		};

		/// <summary>Whether an integer <see cref="Kind" /> is two's-complement signed. Meaningless
		/// (and false) for a non-integer kind.</summary>
		public bool IsSignedInteger => Kind is MetaFieldKind.Int8 or MetaFieldKind.Int16 or MetaFieldKind.Int32 or MetaFieldKind.Int64;

		/// <summary>
		///     Per-component axis labels for a float/vector-shaped field, matching the convention
		///     this codebase's own WPF meta editor already uses (see
		///     Multi2Value.xaml/Multi3Value.xaml's per-<c>Type</c> label triggers): "x/y/z" for a
		///     position (<see cref="MetaFieldKind.Point2" />/<see cref="MetaFieldKind.Point3" />),
		///     "i/j/k[/w]" for a direction or quaternion (<see cref="MetaFieldKind.Vector2" />
		///     through <see cref="MetaFieldKind.Vector4" />, the default for anything 4-wide), and
		///     "yaw/pitch[/roll]" for an orientation (<see cref="MetaFieldKind.Degree2" />/
		///     <see cref="MetaFieldKind.Degree3" />). Empty for anything that is not this shape of
		///     field; length always matches how many floats <see cref="FieldEditState.Floats" />
		///     holds for this <see cref="Kind" />.
		/// </summary>
		public string[] ComponentLabels => Kind switch
		{
			MetaFieldKind.Float32 => new[] { "Value" },
			MetaFieldKind.Degree => new[] { "Angle" },
			MetaFieldKind.Point2 => new[] { "X", "Y" },
			MetaFieldKind.Point3 => new[] { "X", "Y", "Z" },
			MetaFieldKind.Vector2 => new[] { "I", "J" },
			MetaFieldKind.Vector3 => new[] { "I", "J", "K" },
			MetaFieldKind.Vector4 => new[] { "I", "J", "K", "W" },
			MetaFieldKind.Degree2 => new[] { "Yaw", "Pitch" },
			MetaFieldKind.Degree3 => new[] { "Yaw", "Pitch", "Roll" },
			MetaFieldKind.RangeFloat32 or MetaFieldKind.RangeDegree => new[] { "Min", "Max" },
			_ => Array.Empty<string>()
		};
	}
}
