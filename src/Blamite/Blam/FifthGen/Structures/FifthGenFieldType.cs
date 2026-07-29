namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The field types a fifth-generation tag definition can use.
	/// </summary>
	/// <remarks>
	///     A payload's own type table names its types and states their on-disk widths, so a parser never has to guess. This
	///     enumeration exists because some types change how the data walk behaves - a block emits a nested section, a struct is
	///     inlined, an array repeats - and behaviour is easier to switch on than a string.
	/// </remarks>
	public enum FifthGenFieldType
	{
		/// <summary>
		///     A type name which is not in the known vocabulary. Such a field is read using the width its type table entry
		///     declares and is assumed to emit no nested section.
		/// </summary>
		Unknown,

		Array,
		Custom,
		Pad,
		Struct,
		TerminatorX,

		ByteFlags,
		ByteInteger,
		CharBlockIndex,
		CharEnum,
		CharInteger,

		CustomShortBlockIndex,
		ShortBlockIndex,
		ShortEnum,
		ShortInteger,
		WordFlags,
		WordInteger,

		Angle,
		ArgbColor,
		CustomLongBlockIndex,
		DwordInteger,
		LongBlockFlags,
		LongBlockIndex,
		LongEnum,
		LongFlags,
		LongInteger,
		Real,
		RealFraction,
		RgbColor,
		ShortIntegerBounds,
		StringId,
		Tag,

		AngleBounds,
		FractionBounds,
		Int64Integer,
		PageableResource,
		RealBounds,
		RealEulerAngles2D,
		RealPoint2D,
		RealVector2D,
		Rectangle2D,

		ApiInterop,
		Block,
		RealEulerAngles3D,
		RealPlane2D,
		RealPoint3D,
		RealRgbColor,
		RealVector3D,

		RealArgbColor,
		RealPlane3D,
		RealQuaternion,
		TagReference,

		Data,

		String,

		LongString
	}

	/// <summary>
	///     What a field definition's auxiliary word means. It is type-dependent.
	/// </summary>
	public enum FifthGenAuxKind
	{
		/// <summary>The auxiliary word is unused and reads zero.</summary>
		None,

		/// <summary>The auxiliary word is a width in bytes.</summary>
		PaddingWidth,

		/// <summary>The auxiliary word indexes the struct table.</summary>
		StructIndex,

		/// <summary>The auxiliary word indexes the array table.</summary>
		ArrayIndex,

		/// <summary>The auxiliary word indexes the block table.</summary>
		BlockIndex,

		/// <summary>The auxiliary word indexes the enum table.</summary>
		EnumIndex
	}
}
