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
		///     A scalar field of a fifth-generation (Campaign Evolved) tag: the tag's own
		///     <c>blay</c> chunk states its real type name (see <see cref="MetaFieldDef.KindLabelOverride"/>)
		///     and the value was already formatted by <c>FifthGenValueFormatter</c>. Deliberately
		///     not one of the classic scalar kinds above: those are exactly the kinds
		///     <see cref="MetaFieldDef.IsEditable"/> and the sidebar's editor factory agree carry
		///     real edit state, and a fifth-generation field never does (see TagDocumentViewModel).
		/// </summary>
		FifthGenValue
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
			MetaFieldKind.StringId or MetaFieldKind.OldStringId => true,
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
