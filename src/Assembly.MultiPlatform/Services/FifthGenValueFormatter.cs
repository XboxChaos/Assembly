using System;
using System.Globalization;
using System.Linq;
using Blamite.Blam.FifthGen.Structures;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     Formats one already-parsed fifth-generation (Campaign Evolved) field value into the
	///     display string the field table's "FIELD / VALUE" column shows.
	/// </summary>
	/// <remarks>
	///     Everything here is read-only projection: <see cref="Blamite.Blam.FifthGen.FifthGenTagFile" />
	///     parses a whole tag payload eagerly (schema and data both), so by the time a
	///     <see cref="FifthGenTagValue" /> reaches this formatter it is already fully resolved -
	///     stringID text, tag reference paths and nested struct/block/array contents included.
	///     There is nothing left to read from a stream, which is why this takes a value object
	///     rather than a reader and an offset the way <see cref="MetaValueReader" /> does for
	///     classic engines.
	///
	///     Composite "real" types (points, vectors, colours, planes, bounds, rectangles) are
	///     decoded by <see cref="Blamite.Blam.FifthGen.Structures.FifthGenTagDataReader" /> into the
	///     typed value classes in <c>FifthGenCompositeValue.cs</c>, and rendered here from their
	///     decoded components rather than as raw bytes - see those classes' remarks for what each
	///     layout rests on. What genuinely has no known layout (an unrecognized type, or a composite
	///     type whose declared width did not match what its shape needs) still comes through as
	///     <see cref="FifthGenOpaqueValue" /> and is rendered as raw bytes, matching that class's own
	///     remarks: guessing would be worse than handing back the bytes.
	/// </remarks>
	public static class FifthGenValueFormatter
	{
		public static string Format(FifthGenTagValue value)
		{
			switch (value)
			{
				case FifthGenIntegerValue iv:
					return FormatInteger(iv);

				case FifthGenRealValue rv:
					return F(rv.Value);

				case FifthGenVectorValue vv:
					return "(" + string.Join(", ", vv.Components.Select(F)) + ")";

				case FifthGenBoundsValue bv:
					return $"{F(bv.Lo)} to {F(bv.Hi)}";

				case FifthGenIntegerBoundsValue ibv:
					return $"{ibv.Lo} to {ibv.Hi}";

				case FifthGenColorValue cv:
					return FormatPackedColor(cv);

				case FifthGenRealColorValue rcv:
					return "(" + string.Join(", ", rcv.Components.Select(F)) + ")";

				case FifthGenRectangleValue rev:
					return "(" + string.Join(", ", rev.Components) + ")";

				case FifthGenStringValue sv:
					return $"\"{sv.Value}\"";

				case FifthGenStringIDValue sid:
					return $"\"{sid.Value ?? ""}\"  (0x{sid.Id.Value:X8})";

				case FifthGenTagReferenceValue tr:
					return tr.IsNull ? "null" : $"{tr.Path}.{tr.GroupTag}";

				case FifthGenDataValue dv:
					return $"{dv.Contents.Length} byte(s)";

				case FifthGenResourceValue rsv:
					return $"{rsv.Contents.Length} byte(s), {(rsv.IsAttached ? "attached" : "detached")}";

				case FifthGenOpaqueValue ov:
					return FormatBytes(ov.RawData);

				default:
					// Struct/array/block values are rendered by the row walker itself (they carry
					// a child element count, not a single display value); reaching here would mean
					// a new FifthGenTagValue subtype was added to Blamite. Fall back to its own
					// ToString() rather than crash.
					return value.ToString() ?? "";
			}
		}

		private static string FormatInteger(FifthGenIntegerValue iv)
		{
			string rendered = iv.IsSigned ? iv.SignedValue.ToString(CultureInfo.InvariantCulture) : iv.Value.ToString(CultureInfo.InvariantCulture);
			string? option = iv.OptionName;
			return option != null ? $"{rendered}  ({option})" : rendered;
		}

		/// <summary>
		///     Renders a packed-byte colour as its channels plus a hex swatch value - exact and
		///     lossless, since the field's bytes already are the channel bytes. The float-packed
		///     colour ("real rgb/argb color") deliberately gets no equivalent swatch: turning its
		///     components into a byte would mean assuming they are normalized to 0..1, which nothing
		///     observed confirms and an HDR value would silently misrepresent.
		/// </summary>
		private static string FormatPackedColor(FifthGenColorValue cv)
		{
			return cv.HasAlpha
				? $"#{cv.Packed:X8}  (a{cv.A} r{cv.R} g{cv.G} b{cv.B})"
				: $"#{(cv.Packed & 0xFFFFFF):X6}  (r{cv.R} g{cv.G} b{cv.B})";
		}

		private static string FormatBytes(byte[] bytes)
		{
			int n = Math.Min(bytes.Length, 32);
			string hex = string.Join(" ", bytes.Take(n).Select(b => b.ToString("X2")));
			return bytes.Length > n ? $"{hex} ... ({bytes.Length} bytes)" : hex;
		}

		private static string F(float f) => f.ToString("0.######", CultureInfo.InvariantCulture);
	}
}
