using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Util;

namespace Assembly.Avalonia.Services
{
	/// <summary>A field descriptor paired with the value actually read from the cache.</summary>
	public sealed class MetaFieldValue
	{
		public MetaFieldDef Def { get; init; } = null!;
		public string Value { get; init; } = "";
		public string OffsetLabel => $"0x{Def.Offset:X4}";
		public string Indent => new string(' ', Def.Depth * 4);
		public string DisplayName => Indent + (string.IsNullOrEmpty(Def.Name) ? "-" : Def.Name);
		public bool IsComment => Def.Kind == MetaFieldKind.Comment;
		public bool IsBlock => Def.Kind == MetaFieldKind.TagBlock;
	}

	/// <summary>The live count/pointer header of a tag block, as read from the cache.</summary>
	public readonly struct BlockHeader
	{
		public int Count { get; init; }
		public uint PointerRaw { get; init; }

		/// <summary>File offset of element 0, or -1 if the pointer couldn't be resolved (e.g. no MetaArea, or Count is 0).</summary>
		public long BaseFileOffset { get; init; }

		public bool HasElements => Count > 0 && BaseFileOffset >= 0;
	}

	/// <summary>
	///     Reads concrete values for a set of <see cref="MetaFieldDef" />s out of an open
	///     cache file. Read-only; nothing here writes to the cache (see MetaValueWriter for that).
	/// </summary>
	public static class MetaValueReader
	{
		public static List<MetaFieldValue> Read(IReader reader, long baseOffset,
			IReadOnlyList<MetaFieldDef> defs, ICacheFile cache)
		{
			var result = new List<MetaFieldValue>(defs.Count);

			foreach (var def in defs)
			{
				if (def.Kind == MetaFieldKind.TagBlockEnd)
					continue;

				string value;
				try
				{
					value = ReadValue(reader, baseOffset, def, cache);
				}
				catch (Exception ex)
				{
					value = $"<read error: {ex.GetType().Name}>";
				}

				result.Add(new MetaFieldValue { Def = def, Value = value });
			}

			return result;
		}

		/// <summary>
		///     Reads a tag block's live count and pointer, converting the pointer to a file
		///     offset via the cache's meta-area pointer converter (<c>cache.MetaArea</c>) —
		///     the same conversion Blamite's own tag reader uses. This is what makes reflexive
		///     element navigation real rather than a mock: the element index spinner in the UI
		///     drives this, not canned data.
		/// </summary>
		public static BlockHeader ReadBlockHeader(IReader r, long absOffset, ICacheFile cache)
		{
			r.SeekTo(absOffset);
			int count = r.ReadInt32();
			uint ptr = r.ReadUInt32();

			long baseOffset = -1;
			if (count > 0 && cache.MetaArea != null)
			{
				try { baseOffset = cache.MetaArea.PointerToOffset(ptr); }
				catch { baseOffset = -1; }
			}

			return new BlockHeader { Count = count, PointerRaw = ptr, BaseFileOffset = baseOffset };
		}

		/// <summary>Formats a single field's value, reading from <paramref name="baseOffset" /> + <see cref="MetaFieldDef.Offset" />.</summary>
		public static string ReadValue(IReader r, long baseOffset, MetaFieldDef d, ICacheFile cache) => ReadOne(r, baseOffset, d, cache);

		/// <summary>
		///     Reads a field's value into the structured shape <see cref="MetaValueWriter" />
		///     writes back from, for fields where <see cref="MetaFieldDef.IsEditable" /> is true.
		///     Used to seed a row's "original" and "current" edit state when a document is loaded
		///     or a tag block element is navigated to.
		/// </summary>
		public static FieldEditState ReadEditState(IReader r, long baseOffset, MetaFieldDef d)
		{
			long at = baseOffset + d.Offset;
			r.SeekTo(at);

			switch (d.Kind)
			{
				case MetaFieldKind.UInt8: return new FieldEditState { Int = r.ReadByte() };
				case MetaFieldKind.Int8: return new FieldEditState { Int = r.ReadSByte() };
				case MetaFieldKind.UInt16: return new FieldEditState { Int = r.ReadUInt16() };
				case MetaFieldKind.Int16: return new FieldEditState { Int = r.ReadInt16() };
				case MetaFieldKind.UInt32: return new FieldEditState { Int = r.ReadUInt32() };
				case MetaFieldKind.Int32: return new FieldEditState { Int = r.ReadInt32() };
				case MetaFieldKind.UInt64: return new FieldEditState { Int = (long)r.ReadUInt64() };
				case MetaFieldKind.Int64: return new FieldEditState { Int = r.ReadInt64() };

				case MetaFieldKind.Float32:
				case MetaFieldKind.Degree:
					return new FieldEditState { Floats = new[] { r.ReadFloat() } };

				case MetaFieldKind.Point2:
				case MetaFieldKind.Vector2:
				case MetaFieldKind.Degree2:
					return new FieldEditState { Floats = new[] { r.ReadFloat(), r.ReadFloat() } };

				case MetaFieldKind.Point3:
				case MetaFieldKind.Vector3:
				case MetaFieldKind.Degree3:
					return new FieldEditState { Floats = new[] { r.ReadFloat(), r.ReadFloat(), r.ReadFloat() } };

				case MetaFieldKind.Vector4:
					return new FieldEditState { Floats = new[] { r.ReadFloat(), r.ReadFloat(), r.ReadFloat(), r.ReadFloat() } };

				case MetaFieldKind.RangeFloat32:
				case MetaFieldKind.RangeDegree:
					return new FieldEditState { Floats = new[] { r.ReadFloat(), r.ReadFloat() } };

				case MetaFieldKind.RangeInt16:
					return new FieldEditState { Shorts = new[] { r.ReadInt16(), r.ReadInt16() } };

				case MetaFieldKind.Enum:
				case MetaFieldKind.Flags:
					return new FieldEditState { Int = ReadSized(r, d.Size) };

				case MetaFieldKind.ColorInt:
					return new FieldEditState { Int = r.ReadUInt32() };

				case MetaFieldKind.StringId:
				case MetaFieldKind.OldStringId:
					return new FieldEditState { Int = r.ReadUInt32() };

				case MetaFieldKind.Ascii:
					return new FieldEditState { Text = r.ReadAscii(Math.Max(0, d.Size)).TrimEnd('\0') };

				case MetaFieldKind.Utf16:
					return new FieldEditState { Text = r.ReadUTF16(Math.Max(0, d.Size)).TrimEnd('\0') };

				default:
					return new FieldEditState();
			}
		}

		private static string ReadOne(IReader r, long baseOffset, MetaFieldDef d, ICacheFile cache)
		{
			if (d.Kind == MetaFieldKind.Comment)
				return d.Note ?? "";

			long at = baseOffset + d.Offset;
			if (at < 0 || at >= r.Length)
				return "<out of range>";

			r.SeekTo(at);

			switch (d.Kind)
			{
				case MetaFieldKind.UInt8:   return r.ReadByte().ToString();
				case MetaFieldKind.Int8:    return r.ReadSByte().ToString();
				case MetaFieldKind.UInt16:  return r.ReadUInt16().ToString();
				case MetaFieldKind.Int16:   return r.ReadInt16().ToString();
				case MetaFieldKind.UInt32:  return r.ReadUInt32().ToString();
				case MetaFieldKind.Int32:   return r.ReadInt32().ToString();
				case MetaFieldKind.UInt64:  return r.ReadUInt64().ToString();
				case MetaFieldKind.Int64:   return r.ReadInt64().ToString();

				case MetaFieldKind.Float32:
				case MetaFieldKind.Degree:  return F(r.ReadFloat());

				case MetaFieldKind.Undefined: return $"0x{r.ReadUInt32():X8}";

				case MetaFieldKind.Datum:
				{
					var di = DatumIndex.ReadFrom(r);
					return di.IsValid ? di.ToString() : "null";
				}

				case MetaFieldKind.Point2:
				case MetaFieldKind.Vector2:
				case MetaFieldKind.Degree2:
					return $"{F(r.ReadFloat())}, {F(r.ReadFloat())}";

				case MetaFieldKind.Point3:
				case MetaFieldKind.Vector3:
				case MetaFieldKind.Degree3:
					return $"{F(r.ReadFloat())}, {F(r.ReadFloat())}, {F(r.ReadFloat())}";

				case MetaFieldKind.Vector4:
				case MetaFieldKind.Plane3:
					return $"{F(r.ReadFloat())}, {F(r.ReadFloat())}, {F(r.ReadFloat())}, {F(r.ReadFloat())}";

				case MetaFieldKind.Plane2:
					return $"{F(r.ReadFloat())}, {F(r.ReadFloat())}, {F(r.ReadFloat())}";

				case MetaFieldKind.RangeFloat32:
				case MetaFieldKind.RangeDegree:
					return $"{F(r.ReadFloat())} .. {F(r.ReadFloat())}";

				case MetaFieldKind.RangeInt16:
					return $"{r.ReadInt16()} .. {r.ReadInt16()}";

				case MetaFieldKind.Rect16:
					return $"{r.ReadInt16()}, {r.ReadInt16()}, {r.ReadInt16()}, {r.ReadInt16()}";

				case MetaFieldKind.Quat16:
					return $"{r.ReadInt16()}, {r.ReadInt16()}, {r.ReadInt16()}, {r.ReadInt16()}";

				case MetaFieldKind.Point16:
					return $"{r.ReadInt16()}, {r.ReadInt16()}";

				case MetaFieldKind.StringId:
				case MetaFieldKind.OldStringId:
				{
					var sid = new StringID(r.ReadUInt32());
					var s = cache.StringIDs?.GetString(sid);
					return s != null ? $"\"{s}\"" : $"0x{sid.Value:X8}";
				}

				case MetaFieldKind.TagReference:
				{
					if (d.Size == 16)
					{
						int groupMagic = r.ReadInt32();
						r.Skip(8);
						var di = DatumIndex.ReadFrom(r);
						return DescribeTagRef(groupMagic, di, cache);
					}
					else
					{
						var di = DatumIndex.ReadFrom(r);
						return DescribeTagRef(0, di, cache);
					}
				}

				case MetaFieldKind.DataReference:
				{
					r.Skip(12);
					int size = r.ReadInt32();
					uint ptr = r.ReadUInt32();
					return $"{size} bytes @ 0x{ptr:X8}";
				}

				case MetaFieldKind.TagBlock:
				{
					int count = r.ReadInt32();
					uint ptr = r.ReadUInt32();
					return $"{count} entries @ 0x{ptr:X8}   ({d.Note})";
				}

				case MetaFieldKind.ColorInt:
				{
					uint v = r.ReadUInt32();
					return d.Note == "argb"
						? $"#{v:X8}  (a{(v >> 24) & 0xFF} r{(v >> 16) & 0xFF} g{(v >> 8) & 0xFF} b{v & 0xFF})"
						: $"#{v & 0xFFFFFF:X6}";
				}

				case MetaFieldKind.ColorF:
				{
					var parts = new List<string>();
					for (int i = 0; i < d.Size / 4; i++) parts.Add(F(r.ReadFloat()));
					return string.Join(", ", parts);
				}

				case MetaFieldKind.Ascii:
					return "\"" + r.ReadAscii(Math.Max(0, d.Size)).TrimEnd('\0') + "\"";

				case MetaFieldKind.Utf16:
					return "\"" + r.ReadUTF16(Math.Max(0, d.Size)).TrimEnd('\0') + "\"";

				case MetaFieldKind.HexString:
				case MetaFieldKind.RawData:
				{
					int n = Math.Min(Math.Max(0, d.Size), 32);
					var bytes = r.ReadBlock(n);
					var hex = string.Join(" ", bytes.Select(b => b.ToString("X2")));
					return d.Size > n ? $"{hex} ... ({d.Size} bytes)" : hex;
				}

				case MetaFieldKind.Flags:
				{
					long raw = ReadSized(r, d.Size);
					if (d.Choices == null || d.Choices.Count == 0)
						return $"0x{raw:X}";
					var set = d.Choices.Where(c => (raw & c.Value) != 0).Select(c => c.Name).ToList();
					return set.Count > 0 ? $"0x{raw:X}  [{string.Join(", ", set)}]" : $"0x{raw:X}  [none]";
				}

				case MetaFieldKind.Enum:
				{
					long raw = ReadSized(r, d.Size);
					var match = d.Choices?.FirstOrDefault(c => c.Value == raw);
					return match is { Name: not null } m && m.Name.Length > 0
						? $"{raw}  ({m.Name})"
						: raw.ToString();
				}

				case MetaFieldKind.Shader:
				case MetaFieldKind.UnicList:
					return $"0x{r.ReadUInt32():X8}  ({d.Note})";

				default:
					return "";
			}
		}

		private static string DescribeTagRef(int groupMagic, DatumIndex di, ICacheFile cache)
		{
			if (!di.IsValid) return "null";
			string group = groupMagic != 0 ? CharConstant.ToString(groupMagic) : "";
			string? name = null;
			try
			{
				if (cache.FileNames != null && cache.Tags.IsValidIndex(di))
					name = cache.FileNames.GetTagName(di);
			}
			catch { /* index out of range in damaged or partial caches */ }

			if (name != null)
				return string.IsNullOrEmpty(group) ? name : $"{name}.{group}";
			return $"{di}" + (string.IsNullOrEmpty(group) ? "" : $" [{group}]");
		}

		private static long ReadSized(IReader r, int size) => size switch
		{
			1 => r.ReadByte(),
			2 => r.ReadUInt16(),
			4 => r.ReadUInt32(),
			8 => (long)r.ReadUInt64(),
			_ => 0
		};

		private static string F(float f) => f.ToString("0.######", CultureInfo.InvariantCulture);
	}
}
