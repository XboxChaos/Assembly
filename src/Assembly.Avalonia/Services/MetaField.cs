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
		Shader, UnicList
	}

	/// <summary>
	///     A single field declared by a tag definition, flattened out of the visitor
	///     callbacks. Offsets are relative to the start of the tag's meta.
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

		/// <summary>Bit names for flags, or option names for enums.</summary>
		public List<(string Name, long Value)>? Choices { get; init; }

		public string KindLabel => Kind.ToString();
	}
}
