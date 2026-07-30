using System;
using System.Text;
using Blamite.IO;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     The write-back half of the tag meta editor. Mirrors <see cref="MetaValueReader" />'s
	///     switch on <see cref="MetaFieldKind" />, but only for the subset of kinds
	///     <see cref="MetaFieldDef.IsEditable" /> reports true for (fixed-size scalars, vectors,
	///     enums, flags, colours and fixed-size strings — nothing that needs the cache to grow,
	///     which would require MetaAllocator and is out of scope for this pass).
	///
	///     Writes go straight through Blamite's <see cref="IStream" /> at the field's real file
	///     offset (baseOffset + Def.Offset) — the same offset math <see cref="MetaValueReader" />
	///     uses to read it, so a save-then-reload round-trips through actual bytes on disk, not
	///     an in-memory mock.
	/// </summary>
	public static class MetaValueWriter
	{
		public static void Write(IStream s, long baseOffset, MetaFieldDef d, FieldEditState edit)
		{
			long at = baseOffset + d.Offset;
			s.SeekTo(at);

			switch (d.Kind)
			{
				case MetaFieldKind.UInt8: s.WriteByte((byte)edit.RequireInt()); break;
				case MetaFieldKind.Int8: s.WriteSByte((sbyte)edit.RequireInt()); break;
				case MetaFieldKind.UInt16: s.WriteUInt16((ushort)edit.RequireInt()); break;
				case MetaFieldKind.Int16: s.WriteInt16((short)edit.RequireInt()); break;
				case MetaFieldKind.UInt32: s.WriteUInt32((uint)edit.RequireInt()); break;
				case MetaFieldKind.Int32: s.WriteInt32((int)edit.RequireInt()); break;
				case MetaFieldKind.UInt64: s.WriteUInt64((ulong)edit.RequireInt()); break;
				case MetaFieldKind.Int64: s.WriteInt64(edit.RequireInt()); break;

				case MetaFieldKind.Float32:
				case MetaFieldKind.Degree:
					s.WriteFloat(edit.RequireFloats(1)[0]);
					break;

				case MetaFieldKind.Point2:
				case MetaFieldKind.Vector2:
				case MetaFieldKind.Degree2:
					foreach (var f in edit.RequireFloats(2)) s.WriteFloat(f);
					break;

				case MetaFieldKind.Point3:
				case MetaFieldKind.Vector3:
				case MetaFieldKind.Degree3:
					foreach (var f in edit.RequireFloats(3)) s.WriteFloat(f);
					break;

				case MetaFieldKind.Vector4:
					foreach (var f in edit.RequireFloats(4)) s.WriteFloat(f);
					break;

				case MetaFieldKind.RangeFloat32:
				case MetaFieldKind.RangeDegree:
					foreach (var f in edit.RequireFloats(2)) s.WriteFloat(f);
					break;

				case MetaFieldKind.RangeInt16:
					var sh = edit.RequireShorts(2);
					s.WriteInt16(sh[0]);
					s.WriteInt16(sh[1]);
					break;

				case MetaFieldKind.Enum:
				case MetaFieldKind.Flags:
					WriteSized(s, d.Size, edit.RequireInt());
					break;

				case MetaFieldKind.ColorInt:
					s.WriteUInt32((uint)edit.RequireInt());
					break;

				case MetaFieldKind.StringId:
				case MetaFieldKind.OldStringId:
					s.WriteUInt32((uint)edit.RequireInt());
					break;

				case MetaFieldKind.Ascii:
					WriteFixedAscii(s, d.Size, edit.RequireText());
					break;

				case MetaFieldKind.Utf16:
					WriteFixedUtf16(s, d.Size, edit.RequireText());
					break;

				default:
					throw new NotSupportedException($"{d.Kind} is not editable yet.");
			}
		}

		private static void WriteSized(IStream s, int size, long value)
		{
			switch (size)
			{
				case 1: s.WriteByte((byte)value); break;
				case 2: s.WriteUInt16((ushort)value); break;
				case 4: s.WriteUInt32((uint)value); break;
				case 8: s.WriteUInt64((ulong)value); break;
				default: throw new NotSupportedException($"unsupported field size {size}");
			}
		}

		private static void WriteFixedAscii(IStream s, int size, string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text ?? "");
			var buf = new byte[Math.Max(0, size)];
			Array.Copy(bytes, buf, Math.Min(bytes.Length, buf.Length));
			s.WriteBlock(buf);
		}

		private static void WriteFixedUtf16(IStream s, int size, string text)
		{
			int chars = Math.Max(0, size) / 2;
			var buf = new short[chars];
			var src = text ?? "";
			for (int i = 0; i < chars && i < src.Length; i++) buf[i] = (short)src[i];
			foreach (var c in buf) s.WriteInt16(c);
		}
	}

	/// <summary>
	///     The in-flight edited value for one field row, in whichever shape its editor works in.
	///     Compared against a snapshot taken at load time to drive dirty-state and cheap revert
	///     (ctrl+z / "Revert" just restores <see cref="MetaRowViewModel.Original" />).
	/// </summary>
	public sealed class FieldEditState
	{
		public long? Int { get; set; }
		public float[]? Floats { get; set; }
		public short[]? Shorts { get; set; }
		public string? Text { get; set; }

		public long RequireInt() => Int ?? throw new InvalidOperationException("no integer value set");
		public float[] RequireFloats(int n) => Floats?.Length == n ? Floats : throw new InvalidOperationException($"expected {n} float components");
		public short[] RequireShorts(int n) => Shorts?.Length == n ? Shorts : throw new InvalidOperationException($"expected {n} short components");
		public string RequireText() => Text ?? "";

		// Named component accessors so simple XAML TwoWay bindings (TextBox.Text) can address a
		// single component of Floats/Shorts without relying on indexer-binding syntax.
		public float X { get => GetF(0); set => SetF(0, value); }
		public float Y { get => GetF(1); set => SetF(1, value); }
		public float Z { get => GetF(2); set => SetF(2, value); }
		public float W { get => GetF(3); set => SetF(3, value); }
		public float RangeLo { get => GetF(0); set => SetF(0, value); }
		public float RangeHi { get => GetF(1); set => SetF(1, value); }
		private float GetF(int i) => Floats != null && i < Floats.Length ? Floats[i] : 0f;
		private void SetF(int i, float v) { if (Floats != null && i < Floats.Length) Floats[i] = v; }

		public short ShortLo { get => GetS(0); set => SetS(0, value); }
		public short ShortHi { get => GetS(1); set => SetS(1, value); }
		private short GetS(int i) => Shorts != null && i < Shorts.Length ? Shorts[i] : (short)0;
		private void SetS(int i, short v) { if (Shorts != null && i < Shorts.Length) Shorts[i] = v; }

		public FieldEditState Clone() => new()
		{
			Int = Int,
			Floats = Floats == null ? null : (float[])Floats.Clone(),
			Shorts = Shorts == null ? null : (short[])Shorts.Clone(),
			Text = Text
		};

		public bool ValueEquals(FieldEditState other)
		{
			if (Int != other.Int) return false;
			if (Text != other.Text) return false;
			if (!FloatsEqual(Floats, other.Floats)) return false;
			if (!ShortsEqual(Shorts, other.Shorts)) return false;
			return true;
		}

		private static bool FloatsEqual(float[]? a, float[]? b)
		{
			if (a == null || b == null) return a == b;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (Math.Abs(a[i] - b[i]) > 0.0000001f) return false;
			return true;
		}

		private static bool ShortsEqual(short[]? a, short[]? b)
		{
			if (a == null || b == null) return a == b;
			if (a.Length != b.Length) return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i]) return false;
			return true;
		}
	}
}
