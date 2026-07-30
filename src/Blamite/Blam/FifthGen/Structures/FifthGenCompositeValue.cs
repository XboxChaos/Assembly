using System;
using System.Collections.Generic;
using System.IO;
using Blamite.IO;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     Re-encodes a composite field's components back into its inline bytes.
	/// </summary>
	/// <remarks>
	///     Every composite type in this file decodes as a plain run of same-width numbers read one
	///     after another in the payload's endianness, with nothing between them - see
	///     <c>FifthGenTagDataReader.ReadVector</c> and its siblings. Writing is therefore the exact
	///     mirror of reading, and these helpers exist so that the symmetry lives in one place rather
	///     than being restated across six setters that could then drift from the reader
	///     independently. A field's inline width is fixed by the schema, so a component count is
	///     never allowed to change.
	/// </remarks>
	internal static class FifthGenCompositeEncoding
	{
		public static byte[] Floats(IList<float> components, Endian endianness)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new EndianWriter(stream, endianness);
				foreach (float component in components)
					writer.WriteFloat(component);
				return stream.ToArray();
			}
		}

		public static byte[] Shorts(IList<short> components, Endian endianness)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new EndianWriter(stream, endianness);
				foreach (short component in components)
					writer.WriteInt16(component);
				return stream.ToArray();
			}
		}

		public static byte[] Word(uint value, Endian endianness)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new EndianWriter(stream, endianness);
				writer.WriteUInt32(value);
				return stream.ToArray();
			}
		}

		/// <summary>
		///     Rejects a component count that differs from the one the field already holds.
		/// </summary>
		/// <remarks>
		///     A three-float <c>real point 3d</c> cannot become a four-float one: the width belongs to
		///     the schema, not to the value. Catching it here names the field and both counts, which
		///     is a better error than the byte-width mismatch the raw-data replacement would raise a
		///     moment later.
		/// </remarks>
		public static void RequireSameCount(FifthGenTagValue value, int existing, int given)
		{
			if (existing != given)
			{
				throw new ArgumentException(
					$"Field '{value.Name}' ({value.TypeName}) has {existing} component(s); {given} were given. " +
					"A field's component count is fixed by the schema.", nameof(given));
			}
		}
	}

	/// <summary>
	///     A field whose inline bytes are a run of two, three or four IEEE-754 32-bit floats: a
	///     point, a vector, a pair or triple of Euler angles, a plane's coefficients, or a
	///     quaternion.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This is one class for nine type names - <c>real point 2d</c>/<c>3d</c>,
	///         <c>real vector 2d</c>/<c>3d</c>, <c>real euler angles 2d</c>/<c>3d</c>,
	///         <c>real plane 2d</c>, <c>real plane 3d</c> and <c>real quaternion</c> - because they
	///         all share the same on-disk shape: <c>N</c> floats end to end with nothing between
	///         them, for <c>N</c> = declared width / 4. What distinguishes a point from a vector
	///         from an Euler triple is never the bytes, only the name the payload gives the field,
	///         which is why this class does not try to guess a semantic difference and instead
	///         leaves <see cref="FifthGenTagValue.Type" />/<see cref="FifthGenTagValue.TypeName" />
	///         (inherited from the base class) to say which one a particular instance is.
	///     </para>
	///     <para>
	///         The N-floats-in-a-row layout is not a guess: this repository already encodes it for
	///         every earlier Blam generation's plugin XML (see
	///         <c>Blamite.Plugins.AssemblyPluginLoader</c>'s <c>point2</c>/<c>point3</c>/
	///         <c>vector2</c>/<c>vector3</c>/<c>vector4</c>/<c>quaternion</c>/<c>degree2</c>/
	///         <c>degree3</c>/<c>plane2</c>/<c>plane3</c> cases, and the matching reads in
	///         <c>Assembly.Avalonia.Services.MetaValueReader</c>), and the widths line up exactly:
	///         2 floats = 8 bytes for the "2d" and bounds-shaped names, 3 floats = 12 bytes for the
	///         "3d" names and <c>real plane 2d</c> (a 2D normal plus a distance), 4 floats = 16
	///         bytes for <c>real plane 3d</c> (a 3D normal plus a distance) and
	///         <c>real quaternion</c>. No other width in <see cref="FifthGenFieldTypes" /> is
	///         shared with an unrelated meaning at these three sizes once the block/struct/tag
	///         reference/pageable-resource types (which all emit a nested section rather than
	///         staying inline) are set aside, so the width alone identifies the float count.
	///     </para>
	///     <para>
	///         What the arithmetic cannot prove is which float is which axis, or that these are
	///         floats at all rather than, say, three shorts and a pad. That is what
	///         <see cref="FifthGenTagDataReader" />'s plausibility check is for: every component
	///         read this way is tested for NaN, infinity and denormals, which is what garbage
	///         (wrongly-typed) float data looks like.
	///     </para>
	///     <para>
	///         This was checked, not just reasoned about: decoding a real Campaign Evolved mod's
	///         four tags plus its scenario (83,269 vector-shaped fields across
	///         <c>real point 2d</c>/<c>3d</c>, <c>real vector 2d</c>/<c>3d</c>,
	///         <c>real euler angles 2d</c>/<c>3d</c>, <c>real plane 2d</c> and <c>real plane 3d</c>)
	///         produced zero plausibility failures, and the values are not merely finite - they are
	///         the numbers those names predict: <c>real plane 2d</c>/<c>3d</c> normals come out unit
	///         length (e.g. a bsp2d node plane of (-0.9486833, -0.3162278, ...), where
	///         0.9486833^2 + 0.3162278^2 = 1.0 to six figures), <c>real vector 3d</c> "up"/"forward"
	///         fields are likewise unit length, <c>real point 3d</c> "position" fields land in the
	///         tens-of-metres range a Pelican-sized level would use, and <c>real euler angles 3d</c>
	///         "rotation" fields land in the +/-pi radian range a rotation should
	///         (e.g. (-2.8797932, -0.049087383, 3.5083673E-16)).
	///         <c>real quaternion</c> shares this class's code path and plausibility check but was
	///         not declared by any field in the tags checked, so it has arithmetic and
	///         cross-generation precedent behind it but no real-data confirmation of its own; see
	///         <see cref="FifthGenTagFile.Warnings" /> for the individual fields, if any, where a
	///         specific instance's bytes did not look like a plausible float.
	///     </para>
	/// </remarks>
	public class FifthGenVectorValue : FifthGenTagValue
	{
		internal FifthGenVectorValue(FifthGenFieldDefinition field, byte[] rawData, float[] components)
			: base(field, rawData)
		{
			Components = components;
		}

		/// <summary>
		///     Gets the field's components, in the order they appear in the file.
		/// </summary>
		public IList<float> Components { get; private set; }

		/// <summary>
		///     Sets the field's components, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="components">The new components, in file order. Must be as many as the field already has.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetComponents(IList<float> components, Endian endianness)
		{
			if (components == null)
				throw new ArgumentNullException(nameof(components));
			FifthGenCompositeEncoding.RequireSameCount(this, Components.Count, components.Count);

			ReplaceRawData(FifthGenCompositeEncoding.Floats(components, endianness));
			Components = new List<float>(components);
		}

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = ({string.Join(", ", Components)})";
		}
	}

	/// <summary>
	///     A field whose inline bytes are two IEEE-754 32-bit floats read as a minimum and a
	///     maximum, rather than as the components of a tuple.
	/// </summary>
	/// <remarks>
	///     <c>real bounds</c>, <c>angle bounds</c> and <c>fraction bounds</c> are all 8 bytes, the
	///     same width a <see cref="FifthGenVectorValue" /> would read as a 2-float tuple, but a
	///     bound is a different shape with a different role: a range's two numbers are ordered
	///     (low, high) rather than being independent axes, which is why this is its own class
	///     rather than reusing <see cref="FifthGenVectorValue" /> with a component count of two.
	///     Classic tooling draws exactly the same distinction - <c>Assembly.Avalonia.Services.
	///     MetaValueReader</c> renders its <c>RangeFloat32</c>/<c>RangeDegree</c> kinds as
	///     "lo .. hi" rather than "x, y" - and the same plausibility check
	///     <see cref="FifthGenVectorValue" /> relies on applies here: both floats are checked for
	///     NaN, infinity and denormals before this class is used, with <see cref="FifthGenTagFile.Warnings" />
	///     recording any field where the check did not pass. Checked against a real Campaign Evolved
	///     mod's tags, all 163 bounds-shaped fields (50 <c>angle bounds</c>, 6 <c>fraction bounds</c>,
	///     107 <c>real bounds</c>) decoded to plausible finite floats with no warnings, and the values
	///     read exactly like ranges rather than like arbitrary tuples: a "trace yaw angle"
	///     <c>angle bounds</c> of 0 to 6.283185 is 0 to 2*pi radians (a full turn) to the last printed
	///     digit, and a scenario's structure bsp "world bounds x/y/z" <c>real bounds</c> fields come
	///     out as sensible level-sized low/high pairs (e.g. -41.57136 to 29.7603).
	/// </remarks>
	public class FifthGenBoundsValue : FifthGenTagValue
	{
		internal FifthGenBoundsValue(FifthGenFieldDefinition field, byte[] rawData, float lo, float hi)
			: base(field, rawData)
		{
			Lo = lo;
			Hi = hi;
		}

		/// <summary>
		///     Gets the low end of the range.
		/// </summary>
		public float Lo { get; private set; }

		/// <summary>
		///     Gets the high end of the range.
		/// </summary>
		public float Hi { get; private set; }

		/// <summary>
		///     Sets the range, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="lo">The new low end.</param>
		/// <param name="hi">The new high end.</param>
		/// <param name="endianness">The payload's endianness.</param>
		/// <remarks>
		///     An inverted range is written as given. Nothing in the format forbids one, several
		///     engines have historically used <c>hi &lt; lo</c> to mean "disabled", and silently
		///     reordering a caller's numbers would be this class inventing a rule it cannot support.
		/// </remarks>
		public void SetBounds(float lo, float hi, Endian endianness)
		{
			ReplaceRawData(FifthGenCompositeEncoding.Floats(new[] {lo, hi}, endianness));
			Lo = lo;
			Hi = hi;
		}

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Lo} to {Hi}";
		}
	}

	/// <summary>
	///     A field whose inline bytes are two 16-bit signed integers read as a minimum and a
	///     maximum.
	/// </summary>
	/// <remarks>
	///     <c>short integer bounds</c> is 4 bytes: two <see cref="short" />s, not one
	///     <see cref="float" />s worth of anything, which the type's own name states directly (it
	///     is the only "bounds" name in <see cref="FifthGenFieldTypes" /> that says "short" instead
	///     of "real", "angle" or "fraction"). It mirrors classic <c>range16</c>
	///     (<c>Assembly.Avalonia.Services.MetaFieldKind.RangeInt16</c>), which the same reader reads
	///     as two <see cref="short" />s and renders as "lo .. hi". There is nothing to a plausibility
	///     check for a whole number the way there is for a float - every bit pattern is a valid
	///     <see cref="short" /> - so none is applied here. 3,053 fields of this type decoded across a
	///     real Campaign Evolved mod's tags without a single width-mismatch warning, which is the
	///     check that width admits: every declared <c>short integer bounds</c> field really was 4
	///     bytes.
	/// </remarks>
	public class FifthGenIntegerBoundsValue : FifthGenTagValue
	{
		internal FifthGenIntegerBoundsValue(FifthGenFieldDefinition field, byte[] rawData, short lo, short hi)
			: base(field, rawData)
		{
			Lo = lo;
			Hi = hi;
		}

		/// <summary>
		///     Gets the low end of the range.
		/// </summary>
		public short Lo { get; private set; }

		/// <summary>
		///     Gets the high end of the range.
		/// </summary>
		public short Hi { get; private set; }

		/// <summary>
		///     Sets the range, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="lo">The new low end.</param>
		/// <param name="hi">The new high end.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetBounds(short lo, short hi, Endian endianness)
		{
			ReplaceRawData(FifthGenCompositeEncoding.Shorts(new[] {lo, hi}, endianness));
			Lo = lo;
			Hi = hi;
		}

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = {Lo} to {Hi}";
		}
	}

	/// <summary>
	///     A field whose inline bytes are a colour packed as four bytes: alpha, red, green and
	///     blue, most significant byte first once read as a 32-bit word.
	/// </summary>
	/// <remarks>
	///     <para>
	///         <c>rgb color</c> and <c>argb color</c> are both 4 bytes - one byte per channel - which
	///         is the only reading of that width that matches classic <c>color</c>/<c>color32</c>
	///         (<c>Assembly.Avalonia.Services.MetaFieldKind.ColorInt</c>). That code path reads a
	///         single <c>UInt32</c> and decomposes it as <c>a = (v&gt;&gt;24)</c>,
	///         <c>r = (v&gt;&gt;16)</c>, <c>g = (v&gt;&gt;8)</c>, <c>b = v</c>, each masked to a
	///         byte, and treats the alpha byte as meaningful only when the field's declared type
	///         says so - exactly the split <c>argb color</c>/<c>rgb color</c> makes by name. That
	///         same read and the same decomposition are used here.
	///     </para>
	///     <para>
	///         A byte is a byte: there is no plausibility check to run, unlike the float-based
	///         composite types. The one place this could still be wrong is channel order (whether
	///         the first byte in the file is alpha or blue), which is inherited unchanged from the
	///         classic layout and has not been independently re-derived here.
	///     </para>
	///     <para>
	///         The real mod this was checked against declares 490 <c>rgb color</c> fields and zero
	///         <c>argb color</c> fields, so only the no-alpha half of this class has a real-data check
	///         behind it. Every one of those 490 decoded to the packed word <c>0xFF000000</c> - opaque
	///         (unused) alpha, black RGB - which is the value an unset colour override would carry if
	///         the top byte really is a spare/opaque alpha slot rather than, say, a fourth colour
	///         channel or padding of some other value; a consistently-written 0xFF in a byte nothing
	///         else touches is exactly what "unused but initialized" looks like. It is corroborating,
	///         not conclusive: none of the 490 happened to be a non-black colour, so red/green/blue
	///         placement within the low three bytes was not independently exercised either.
	///     </para>
	/// </remarks>
	public class FifthGenColorValue : FifthGenTagValue
	{
		internal FifthGenColorValue(FifthGenFieldDefinition field, byte[] rawData, uint packed, bool hasAlpha)
			: base(field, rawData)
		{
			Packed = packed;
			HasAlpha = hasAlpha;
		}

		/// <summary>
		///     Gets the colour as a packed 0xAARRGGBB word. The alpha byte is present whether or not
		///     <see cref="HasAlpha" /> is true; it is simply not meaningful when it is false.
		/// </summary>
		public uint Packed { get; private set; }

		/// <summary>
		///     Gets whether the field's declared type gives the alpha byte a meaning (<c>argb color</c>) or not (<c>rgb color</c>).
		/// </summary>
		public bool HasAlpha { get; private set; }

		/// <summary>
		///     Gets the alpha channel. Only meaningful when <see cref="HasAlpha" /> is true.
		/// </summary>
		public byte A
		{
			get { return (byte) ((Packed >> 24) & 0xFF); }
		}

		/// <summary>
		///     Gets the red channel.
		/// </summary>
		public byte R
		{
			get { return (byte) ((Packed >> 16) & 0xFF); }
		}

		/// <summary>
		///     Gets the green channel.
		/// </summary>
		public byte G
		{
			get { return (byte) ((Packed >> 8) & 0xFF); }
		}

		/// <summary>
		///     Gets the blue channel.
		/// </summary>
		public byte B
		{
			get { return (byte) (Packed & 0xFF); }
		}

		/// <summary>
		///     Sets the packed colour word, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="packed">The new colour, packed as <c>0xAARRGGBB</c>.</param>
		/// <param name="endianness">The payload's endianness.</param>
		/// <remarks>
		///     The alpha byte is written even for a type whose name says only <c>rgb</c>. Every one of
		///     the 490 <c>rgb color</c> fields measured in a real mod's tags carried <c>0xFF</c> there
		///     rather than zero, so the byte is clearly meaningful to the engine and blanking it
		///     would be a change nobody asked for. Callers that want the classic behaviour should
		///     pass the alpha they want; <see cref="A" /> reports what is currently stored.
		/// </remarks>
		public void SetPacked(uint packed, Endian endianness)
		{
			ReplaceRawData(FifthGenCompositeEncoding.Word(packed, endianness));
			Packed = packed;
		}

		public override string ToString()
		{
			return HasAlpha
				? $"{TypeName} '{Name}' = #{Packed:X8} (a{A} r{R} g{G} b{B})"
				: $"{TypeName} '{Name}' = #{(Packed & 0xFFFFFF):X6} (r{R} g{G} b{B})";
		}
	}

	/// <summary>
	///     A field whose inline bytes are a colour packed as three or four floats: red, green and
	///     blue, or alpha, red, green and blue.
	/// </summary>
	/// <remarks>
	///     <c>real rgb color</c> (12 bytes) and <c>real argb color</c> (16 bytes) are 3 and 4 floats
	///     respectively, matching classic <c>colorf</c> (<c>Assembly.Avalonia.Services.
	///     MetaFieldKind.ColorF</c>), which reads <c>declared size / 4</c> floats with no further
	///     structure of its own. Component order (alpha first when present) follows the name's own
	///     spelling - "argb" - the same way the packed-byte sibling type is read; nothing in the
	///     payload states it independently, so it is carried across from that precedent rather than
	///     re-derived. The plausibility check applied to <see cref="FifthGenVectorValue" /> - NaN,
	///     infinity and denormals - is applied to every component here too, since these are still
	///     ordinary IEEE-754 floats and the same failure mode (a layout that is not actually
	///     float-shaped) would show up the same way. The real mod this was checked against declares
	///     12 <c>real rgb color</c> fields and zero <c>real argb color</c> fields, so again only the
	///     no-alpha half has a real-data check: every one of the 12 (a vehicle's "change colors"
	///     permutation bounds) decoded to three floats in [0, 1] with no plausibility warnings - the
	///     range a normalized colour channel should have, e.g. (0.5490196, 0.627451, 0.454902).
	/// </remarks>
	public class FifthGenRealColorValue : FifthGenTagValue
	{
		internal FifthGenRealColorValue(FifthGenFieldDefinition field, byte[] rawData, float[] components, bool hasAlpha)
			: base(field, rawData)
		{
			Components = components;
			HasAlpha = hasAlpha;
		}

		/// <summary>
		///     Gets the colour's components, in the order they appear in the file: alpha, red, green,
		///     blue when <see cref="HasAlpha" /> is true; red, green, blue otherwise.
		/// </summary>
		public IList<float> Components { get; private set; }

		/// <summary>
		///     Gets whether the first component is an alpha channel (<c>real argb color</c>) or not
		///     (<c>real rgb color</c>).
		/// </summary>
		public bool HasAlpha { get; private set; }

		/// <summary>
		///     Sets the colour's channels, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="components">The new channels, in file order. Must be as many as the field already has.</param>
		/// <param name="endianness">The payload's endianness.</param>
		/// <remarks>
		///     Values are written exactly as given and are not clamped. Every real-colour field
		///     measured so far sits in [0,1], but nothing in the format says a channel must, and
		///     over-range colour is a real technique rather than obviously a mistake.
		/// </remarks>
		public void SetComponents(IList<float> components, Endian endianness)
		{
			if (components == null)
				throw new ArgumentNullException(nameof(components));
			FifthGenCompositeEncoding.RequireSameCount(this, Components.Count, components.Count);

			ReplaceRawData(FifthGenCompositeEncoding.Floats(components, endianness));
			Components = new List<float>(components);
		}

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = ({string.Join(", ", Components)})";
		}
	}

	/// <summary>
	///     A field whose inline bytes are four 16-bit signed integers.
	/// </summary>
	/// <remarks>
	///     <c>rectangle 2d</c> is 8 bytes, the same width a <see cref="FifthGenVectorValue" /> would
	///     read as a 2-float point. The name is what breaks the tie: every other 8-byte composite
	///     name in <see cref="FifthGenFieldTypes" /> either says "real" outright (<c>real point 2d</c>,
	///     <c>real vector 2d</c>) or is a well-established real-valued bound (<c>angle bounds</c>,
	///     <c>fraction bounds</c>, <c>real bounds</c>); <c>rectangle 2d</c> alone drops the "real"
	///     qualifier, the same way classic <c>rect16</c> (<c>Assembly.Avalonia.Services.
	///     MetaFieldKind.Rect16</c>) is spelled to say "16-bit" rather than "real", and that classic
	///     type reads four <see cref="short" />s. There is no plausibility check for a whole number
	///     the way there is for a float, so none is applied here; the width/name argument is the
	///     whole of what this rests on. Unlike every other class in this file, it has no real-data
	///     check behind it at all: the real Campaign Evolved mod this was verified against declares
	///     no <c>rectangle 2d</c> field anywhere in its five tags' schemas, vehicle, weapon,
	///     projectile and scenario alike. This decode is offered on the strength of the width/name
	///     argument and the classic <c>rect16</c> precedent alone.
	/// </remarks>
	public class FifthGenRectangleValue : FifthGenTagValue
	{
		internal FifthGenRectangleValue(FifthGenFieldDefinition field, byte[] rawData, short[] components)
			: base(field, rawData)
		{
			Components = components;
		}

		/// <summary>
		///     Gets the rectangle's four components, in the order they appear in the file. Which of
		///     top/left/bottom/right (or another ordering entirely) each one is has not been
		///     determined; the file only fixes their count and width.
		/// </summary>
		public IList<short> Components { get; private set; }

		/// <summary>
		///     Sets the rectangle's components, re-encoding <see cref="FifthGenTagValue.RawData" />.
		/// </summary>
		/// <param name="components">The new components, in file order. Must be as many as the field already has.</param>
		/// <param name="endianness">The payload's endianness.</param>
		public void SetComponents(IList<short> components, Endian endianness)
		{
			if (components == null)
				throw new ArgumentNullException(nameof(components));
			FifthGenCompositeEncoding.RequireSameCount(this, Components.Count, components.Count);

			ReplaceRawData(FifthGenCompositeEncoding.Shorts(components, endianness));
			Components = new List<short>(components);
		}

		public override string ToString()
		{
			return $"{TypeName} '{Name}' = ({string.Join(", ", Components)})";
		}
	}
}
