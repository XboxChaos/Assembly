using System;
using System.Globalization;
using System.Linq;
using Blamite.Blam.FifthGen.Structures;

namespace Assembly.Avalonia.Services
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
	///     Composite "real" types (points, colours, planes, bounds) are deliberately rendered as
	///     raw bytes here, matching <see cref="FifthGenOpaqueValue" />'s own remarks: the format
	///     states their width but never their internal layout, so guessing which bytes are which
	///     component would be fabricating behaviour nobody observed.
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

		private static string FormatBytes(byte[] bytes)
		{
			int n = Math.Min(bytes.Length, 32);
			string hex = string.Join(" ", bytes.Take(n).Select(b => b.ToString("X2")));
			return bytes.Length > n ? $"{hex} ... ({bytes.Length} bytes)" : hex;
		}

		private static string F(float f) => f.ToString("0.######", CultureInfo.InvariantCulture);
	}
}
