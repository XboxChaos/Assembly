using System.Collections.Generic;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The field type vocabulary a fifth-generation tag definition draws on, and the rules that follow from it.
	/// </summary>
	/// <remarks>
	///     Fifty-four type names appear across the shipped tag groups and each one has the same on-disk width in every group.
	///     A payload states those widths itself, so the widths here are a cross-check rather than the source of truth: a
	///     disagreement means either the parse has drifted or the engine has changed, and both are worth a warning.
	/// </remarks>
	public static class FifthGenFieldTypes
	{
		private static readonly Dictionary<string, FifthGenFieldType> _typesByName;
		private static readonly Dictionary<FifthGenFieldType, string> _namesByType;
		private static readonly Dictionary<FifthGenFieldType, int> _sizesByType;
		private static readonly HashSet<FifthGenFieldType> _integerTypes;
		private static readonly HashSet<FifthGenFieldType> _signedIntegerTypes;
		private static readonly HashSet<FifthGenFieldType> _realTypes;
		private static readonly HashSet<FifthGenFieldType> _vectorTypes;
		private static readonly HashSet<FifthGenFieldType> _realBoundsTypes;
		private static readonly HashSet<FifthGenFieldType> _integerBoundsTypes;
		private static readonly HashSet<FifthGenFieldType> _packedColorTypes;
		private static readonly HashSet<FifthGenFieldType> _realColorTypes;
		private static readonly HashSet<FifthGenFieldType> _rectangleTypes;

		static FifthGenFieldTypes()
		{
			_typesByName = new Dictionary<string, FifthGenFieldType>();
			_namesByType = new Dictionary<FifthGenFieldType, string>();
			_sizesByType = new Dictionary<FifthGenFieldType, int>();

			// Width 0: the types whose real width is computed from the definition tables rather than declared.
			Define("array", FifthGenFieldType.Array, 0);
			Define("custom", FifthGenFieldType.Custom, 0);
			Define("pad", FifthGenFieldType.Pad, 0);
			Define("struct", FifthGenFieldType.Struct, 0);
			Define("terminator X", FifthGenFieldType.TerminatorX, 0);

			Define("byte flags", FifthGenFieldType.ByteFlags, 1);
			Define("byte integer", FifthGenFieldType.ByteInteger, 1);
			Define("char block index", FifthGenFieldType.CharBlockIndex, 1);
			Define("char enum", FifthGenFieldType.CharEnum, 1);
			Define("char integer", FifthGenFieldType.CharInteger, 1);

			Define("custom short block index", FifthGenFieldType.CustomShortBlockIndex, 2);
			Define("short block index", FifthGenFieldType.ShortBlockIndex, 2);
			Define("short enum", FifthGenFieldType.ShortEnum, 2);
			Define("short integer", FifthGenFieldType.ShortInteger, 2);
			Define("word flags", FifthGenFieldType.WordFlags, 2);
			Define("word integer", FifthGenFieldType.WordInteger, 2);

			Define("angle", FifthGenFieldType.Angle, 4);
			Define("argb color", FifthGenFieldType.ArgbColor, 4);
			Define("custom long block index", FifthGenFieldType.CustomLongBlockIndex, 4);
			Define("dword integer", FifthGenFieldType.DwordInteger, 4);
			Define("long block flags", FifthGenFieldType.LongBlockFlags, 4);
			Define("long block index", FifthGenFieldType.LongBlockIndex, 4);
			Define("long enum", FifthGenFieldType.LongEnum, 4);
			Define("long flags", FifthGenFieldType.LongFlags, 4);
			Define("long integer", FifthGenFieldType.LongInteger, 4);
			Define("real", FifthGenFieldType.Real, 4);
			Define("real fraction", FifthGenFieldType.RealFraction, 4);
			Define("rgb color", FifthGenFieldType.RgbColor, 4);
			Define("short integer bounds", FifthGenFieldType.ShortIntegerBounds, 4);
			Define("string id", FifthGenFieldType.StringId, 4);
			Define("tag", FifthGenFieldType.Tag, 4);

			Define("angle bounds", FifthGenFieldType.AngleBounds, 8);
			Define("fraction bounds", FifthGenFieldType.FractionBounds, 8);
			Define("int64 integer", FifthGenFieldType.Int64Integer, 8);
			Define("pageable resource", FifthGenFieldType.PageableResource, 8);
			Define("real bounds", FifthGenFieldType.RealBounds, 8);
			Define("real euler angles 2d", FifthGenFieldType.RealEulerAngles2D, 8);
			Define("real point 2d", FifthGenFieldType.RealPoint2D, 8);
			Define("real vector 2d", FifthGenFieldType.RealVector2D, 8);
			Define("rectangle 2d", FifthGenFieldType.Rectangle2D, 8);

			Define("api interop", FifthGenFieldType.ApiInterop, 12);
			Define("block", FifthGenFieldType.Block, 12);
			Define("real euler angles 3d", FifthGenFieldType.RealEulerAngles3D, 12);
			Define("real plane 2d", FifthGenFieldType.RealPlane2D, 12);
			Define("real point 3d", FifthGenFieldType.RealPoint3D, 12);
			Define("real rgb color", FifthGenFieldType.RealRgbColor, 12);
			Define("real vector 3d", FifthGenFieldType.RealVector3D, 12);

			Define("real argb color", FifthGenFieldType.RealArgbColor, 16);
			Define("real plane 3d", FifthGenFieldType.RealPlane3D, 16);
			Define("real quaternion", FifthGenFieldType.RealQuaternion, 16);
			Define("tag reference", FifthGenFieldType.TagReference, 16);

			Define("data", FifthGenFieldType.Data, 20);
			Define("string", FifthGenFieldType.String, 32);
			Define("long string", FifthGenFieldType.LongString, 256);

			// Types whose inline bytes are a single whole number. Enumerations, flag words and block indexes are
			// included: they are integers with a name attached. The composite types - points, bounds, colours,
			// planes - are not here; see _vectorTypes, _realBoundsTypes, _integerBoundsTypes, _packedColorTypes,
			// _realColorTypes and _rectangleTypes below, which classify those instead.
			_integerTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.ByteFlags,
				FifthGenFieldType.ByteInteger,
				FifthGenFieldType.CharBlockIndex,
				FifthGenFieldType.CharEnum,
				FifthGenFieldType.CharInteger,
				FifthGenFieldType.CustomShortBlockIndex,
				FifthGenFieldType.ShortBlockIndex,
				FifthGenFieldType.ShortEnum,
				FifthGenFieldType.ShortInteger,
				FifthGenFieldType.WordFlags,
				FifthGenFieldType.WordInteger,
				FifthGenFieldType.CustomLongBlockIndex,
				FifthGenFieldType.DwordInteger,
				FifthGenFieldType.LongBlockFlags,
				FifthGenFieldType.LongBlockIndex,
				FifthGenFieldType.LongEnum,
				FifthGenFieldType.LongFlags,
				FifthGenFieldType.LongInteger,
				FifthGenFieldType.Tag,
				FifthGenFieldType.Int64Integer
			};

			// Block indexes and enumerations use -1 for "none", so they are read as signed.
			_signedIntegerTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.CharBlockIndex,
				FifthGenFieldType.CharEnum,
				FifthGenFieldType.CharInteger,
				FifthGenFieldType.CustomShortBlockIndex,
				FifthGenFieldType.ShortBlockIndex,
				FifthGenFieldType.ShortEnum,
				FifthGenFieldType.ShortInteger,
				FifthGenFieldType.CustomLongBlockIndex,
				FifthGenFieldType.LongBlockIndex,
				FifthGenFieldType.LongEnum,
				FifthGenFieldType.LongInteger,
				FifthGenFieldType.Int64Integer
			};

			_realTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.Angle,
				FifthGenFieldType.Real,
				FifthGenFieldType.RealFraction
			};

			// Two, three or four floats end to end: a point, a vector, an Euler pair/triple, a plane's
			// coefficients or a quaternion. See FifthGenVectorValue's remarks for the width arithmetic and the
			// classic-plugin precedent this rests on.
			_vectorTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.RealPoint2D,
				FifthGenFieldType.RealVector2D,
				FifthGenFieldType.RealEulerAngles2D,
				FifthGenFieldType.RealPoint3D,
				FifthGenFieldType.RealVector3D,
				FifthGenFieldType.RealEulerAngles3D,
				FifthGenFieldType.RealPlane2D,
				FifthGenFieldType.RealPlane3D,
				FifthGenFieldType.RealQuaternion
			};

			// Two floats read as (low, high) rather than as tuple components. See FifthGenBoundsValue's remarks.
			_realBoundsTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.RealBounds,
				FifthGenFieldType.AngleBounds,
				FifthGenFieldType.FractionBounds
			};

			// Two shorts read as (low, high). See FifthGenIntegerBoundsValue's remarks.
			_integerBoundsTypes = new HashSet<FifthGenFieldType> {FifthGenFieldType.ShortIntegerBounds};

			// A colour packed as four bytes. See FifthGenColorValue's remarks.
			_packedColorTypes = new HashSet<FifthGenFieldType> {FifthGenFieldType.RgbColor, FifthGenFieldType.ArgbColor};

			// A colour packed as three or four floats. See FifthGenRealColorValue's remarks.
			_realColorTypes = new HashSet<FifthGenFieldType>
			{
				FifthGenFieldType.RealRgbColor,
				FifthGenFieldType.RealArgbColor
			};

			// Four shorts. See FifthGenRectangleValue's remarks for why this is shorts and not floats despite
			// sharing an 8-byte width with a 2-float vector.
			_rectangleTypes = new HashSet<FifthGenFieldType> {FifthGenFieldType.Rectangle2D};
		}

		/// <summary>
		///     Gets the number of type names in the known vocabulary.
		/// </summary>
		public static int KnownTypeCount
		{
			get { return _typesByName.Count; }
		}

		/// <summary>
		///     Looks up a type name from a payload's type table.
		/// </summary>
		/// <param name="name">The type name as it appears in the string blob.</param>
		/// <returns>
		///     The matching type, or <see cref="FifthGenFieldType.Unknown" /> if the name is not in the known vocabulary.
		/// </returns>
		public static FifthGenFieldType Parse(string name)
		{
			FifthGenFieldType result;
			if (name != null && _typesByName.TryGetValue(name, out result))
				return result;
			return FifthGenFieldType.Unknown;
		}

		/// <summary>
		///     Gets the canonical name of a field type.
		/// </summary>
		/// <param name="type">The type to name.</param>
		/// <returns>The type's name as it appears in a payload, or <c>null</c> if it has none.</returns>
		public static string GetName(FifthGenFieldType type)
		{
			string result;
			if (_namesByType.TryGetValue(type, out result))
				return result;
			return null;
		}

		/// <summary>
		///     Gets the on-disk width a field type is expected to have.
		/// </summary>
		/// <param name="type">The type to measure.</param>
		/// <returns>The expected width in bytes, or -1 if the type is not in the known vocabulary.</returns>
		public static int GetKnownSize(FifthGenFieldType type)
		{
			int result;
			if (_sizesByType.TryGetValue(type, out result))
				return result;
			return -1;
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a single whole number.
		/// </summary>
		public static bool IsInteger(FifthGenFieldType type)
		{
			return _integerTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a signed whole number.
		/// </summary>
		public static bool IsSignedInteger(FifthGenFieldType type)
		{
			return _signedIntegerTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a single 32-bit float.
		/// </summary>
		public static bool IsReal(FifthGenFieldType type)
		{
			return _realTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a run of two, three or four 32-bit floats: a point,
		///     a vector, an Euler pair/triple, a plane or a quaternion. See <see cref="FifthGenVectorValue" />.
		/// </summary>
		public static bool IsVector(FifthGenFieldType type)
		{
			return _vectorTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are two 32-bit floats read as (low, high). See
		///     <see cref="FifthGenBoundsValue" />.
		/// </summary>
		public static bool IsRealBounds(FifthGenFieldType type)
		{
			return _realBoundsTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are two 16-bit integers read as (low, high). See
		///     <see cref="FifthGenIntegerBoundsValue" />.
		/// </summary>
		public static bool IsIntegerBounds(FifthGenFieldType type)
		{
			return _integerBoundsTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a colour packed as four bytes. See
		///     <see cref="FifthGenColorValue" />.
		/// </summary>
		public static bool IsPackedColor(FifthGenFieldType type)
		{
			return _packedColorTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are a colour packed as three or four 32-bit floats. See
		///     <see cref="FifthGenRealColorValue" />.
		/// </summary>
		public static bool IsRealColor(FifthGenFieldType type)
		{
			return _realColorTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a field type's inline bytes are four 16-bit integers. See
		///     <see cref="FifthGenRectangleValue" />.
		/// </summary>
		public static bool IsRectangle(FifthGenFieldType type)
		{
			return _rectangleTypes.Contains(type);
		}

		/// <summary>
		///     Determines whether a packed-colour field type's alpha channel is meaningful.
		/// </summary>
		public static bool HasAlpha(FifthGenFieldType type)
		{
			return type == FifthGenFieldType.ArgbColor || type == FifthGenFieldType.RealArgbColor;
		}

		/// <summary>
		///     Determines what a field's auxiliary word means.
		/// </summary>
		/// <param name="type">The field's type.</param>
		/// <param name="typeName">
		///     The type's name from the payload, used to classify names which are not in the known vocabulary.
		/// </param>
		/// <returns>The meaning of the field's auxiliary word.</returns>
		/// <remarks>
		///     The suffix tests are ordered so that a block index wins over the flags and enumeration suffixes. That leaves
		///     <c>long block flags</c> classified as an enumeration table index, which is what a literal reading of the rule gives,
		///     but it is worth flagging as an assumption: the name could just as reasonably describe a bitfield over a block's
		///     elements, in which case it would index the block table instead. Nothing has been checked either way, so an
		///     auxiliary word which does not resolve is reported as a warning and left unlinked rather than treated as fatal.
		/// </remarks>
		public static FifthGenAuxKind GetAuxKind(FifthGenFieldType type, string typeName)
		{
			switch (type)
			{
				case FifthGenFieldType.Pad:
					return FifthGenAuxKind.PaddingWidth;
				case FifthGenFieldType.Struct:
					return FifthGenAuxKind.StructIndex;
				case FifthGenFieldType.Array:
					return FifthGenAuxKind.ArrayIndex;
				case FifthGenFieldType.Block:
					return FifthGenAuxKind.BlockIndex;
			}

			string name = typeName ?? GetName(type);
			if (name == null)
				return FifthGenAuxKind.None;

			if (name.EndsWith("block index"))
				return FifthGenAuxKind.BlockIndex;
			if (name.EndsWith("enum") || name.EndsWith("flags"))
				return FifthGenAuxKind.EnumIndex;

			return FifthGenAuxKind.None;
		}

		/// <summary>
		///     Gets the magic number of the nested data section a field type emits into the payload.
		/// </summary>
		/// <param name="type">The field's type.</param>
		/// <returns>The section magic, or 0 if the type's value is entirely inline.</returns>
		/// <remarks>
		///     A pageable resource is deliberately absent here: its magic varies with whether the resource is attached, so it is
		///     matched by mask instead. See <see cref="IsPageableResourceSection" />.
		/// </remarks>
		public static int GetSectionMagic(FifthGenFieldType type)
		{
			switch (type)
			{
				case FifthGenFieldType.Block:
					return CharConstant.FromString("tgbl");
				case FifthGenFieldType.Struct:
					return CharConstant.FromString("tgst");
				case FifthGenFieldType.StringId:
					return CharConstant.FromString("tgsi");
				case FifthGenFieldType.Data:
					return CharConstant.FromString("tgda");
				case FifthGenFieldType.TagReference:
					return CharConstant.FromString("tgrf");
				default:
					return 0;
			}
		}

		/// <summary>
		///     Determines whether a section magic belongs to a pageable resource.
		/// </summary>
		/// <param name="magic">The section magic to test.</param>
		/// <returns><c>true</c> if the magic is a pageable resource section.</returns>
		/// <remarks>
		///     The magic reads <c>tg?c</c>, where the third character is <c>r</c> when the resource is attached and NUL when it is
		///     not, so only the outer three characters are stable.
		/// </remarks>
		public static bool IsPageableResourceSection(int magic)
		{
			return (magic & unchecked((int) 0xFFFF00FF)) == (('t' << 24) | ('g' << 16) | 'c');
		}

		/// <summary>
		///     Determines whether a pageable resource section carries an attached resource.
		/// </summary>
		/// <param name="magic">The section magic.</param>
		/// <returns><c>true</c> if the resource is attached.</returns>
		public static bool IsResourceAttached(int magic)
		{
			return ((magic >> 8) & 0xFF) == 'r';
		}

		private static void Define(string name, FifthGenFieldType type, int size)
		{
			_typesByName.Add(name, type);
			_namesByType.Add(type, name);
			_sizesByType.Add(type, size);
		}
	}
}
