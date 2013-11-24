namespace Blamite.Flexibility
{
	/// <summary>
	///     Value types for vertex elements.
	/// </summary>
	public enum VertexElementType
	{
		Float1, // 1D float vector
		Float2, // 2D float vector
		Float3, // 3D float vector
		Float4, // 4D float vector
		Int1, // 1D int vector
		Int2, // 2D int vector
		Int4, // 4D int vector
		UInt1, // 1D unsigned int vector
		UInt2, // 2D unsigned int vector
		UInt4, // 4D unsigned int vector
		Int1N, // 1D int vector, normalized
		Int2N, // 2D int vector, normalized
		Int4N, // 4D int vector, normalized
		UInt1N, // 1D unsigned int vector, normalized
		UInt2N, // 2D unsigned int vector, normalized
		UInt4N, // 4D unsigned int vector, normalized
		D3DColor, // 4 unsigned bytes in the order ARGB, expanded to (R, G, B, A) and normalized by dividing by 255.0f
		UByte4, // 4D unsigned byte vector
		Byte4, // 4D byte vector
		UByte4N, // 4D unsigned byte vector, normalized
		Byte4N, // 4D byte vector, normalized
		Short2, // 2D short vector
		Short4, // 4D short vector
		UShort2, // 2D unsigned short vector
		UShort4, // 4D unsigned short vector
		Short2N, // 2D short vector, normalized
		Short4N, // 4D short vector, normalized
		UShort2N, // 2D unsigned short vector, normalized
		UShort4N, // 4D unsigned short vector, normalized
		UDec3, // 3D unsigned 10-bit 10-bit 10-bit vector
		Dec3, // 3D 10-bit 10-bit 10-bit vector
		UDec3N, // 3D unsigned 10-bit 10-bit 10-bit vector, normalized by dividing by 1023.0f
		Dec3N, // 3D 10-bit 10-bit 10-bit vector, normalized by dividing by 511.0f
		UDec4, // 4D unsigned 10-bit 10-bit 10-bit 2-bit vector
		Dec4, // 4D 10-bit 10-bit 10-bit 2-bit vector
		UDec4N, // 4D unsigned 10-bit 10-bit 10-bit 2-bit vector, normalized by dividing by (1023.0f, 1023.0f, 1023.0f, 3.0f)
		Dec4N, // 4D 10-bit 10-bit 10-bit 2-bit vector, normalized by dividing by (511.0f, 511.0f, 511.0f, 1.0f)
		UHenD3, // 3D unsigned 11-bit 11-bit 10-bit vector
		HenD3, // 3D 11-bit 11-bit 10-bit vector
		UHenD3N, // 3D unsigned 11-bit 11-bit 10-bit vector, normalized by dividing by (2047.0f, 2047.0f, 1023.0f)
		HenD3N, // 3D 11-bit 11-bit 10-bit vector, normalized by dividing by (1023.0f, 1023.0f, 511.0f)
		UDHen3, // 3D unsigned 10-bit 11-bit 11-bit vector
		DHen3, // 3D 10-bit 11-bit 11-bit vector
		UDHen3N, // 3D unsigned 10-bit 11-bit 11-bit vector, normalized by dividing by (1023.0f, 2047.0f, 2047.0f)
		DHen3N, // 3D 10-bit 11-bit 11-bit vector, normalized by dividing by (511.0f, 1023.0f, 1023.0f)
		Float16_2, // 2D 16-bit float vector
		Float16_4 // 4D 16-bit float vector
	}

	/// <summary>
	///     Usage types for vertex elements.
	/// </summary>
	/// <remarks>
	///     For more information, see http://msdn.microsoft.com/en-us/library/windows/desktop/bb172534%28v=vs.85%29.aspx.
	/// </remarks>
	public enum VertexElementUsage
	{
		Position,
		BlendWeight,
		BlendIndices,
		Normal,
		PSize,
		TexCoords,
		Tangent,
		Binormal,
		TessFactor,
		PositionT,
		Color,
		Fog,
		Depth,
		Sample
	}

	/// <summary>
	///     Defines the layout of an element in a vertex.
	/// </summary>
	/// <remarks>
	///     For more information, see http://msdn.microsoft.com/en-us/library/windows/desktop/bb172630%28v=vs.85%29.aspx.
	/// </remarks>
	public class VertexElementLayout
	{
		public VertexElementLayout(int stream, int offset, VertexElementType type, VertexElementUsage usage, int usageIndex)
		{
			Stream = stream;
			Offset = offset;
			Type = type;
			Usage = usage;
			UsageIndex = usageIndex;
		}

		/// <summary>
		///     Gets the index of the stream that the vertex belongs to.
		/// </summary>
		public int Stream { get; private set; }

		/// <summary>
		///     Gets the offset of the element from the start of a vertex.
		/// </summary>
		public int Offset { get; private set; }

		/// <summary>
		///     Gets the type of the element.
		/// </summary>
		public VertexElementType Type { get; private set; }

		/// <summary>
		///     Gets what the element is used for.
		/// </summary>
		public VertexElementUsage Usage { get; private set; }

		/// <summary>
		///     Gets the modifier for <see cref="Usage" />.
		/// </summary>
		public int UsageIndex { get; private set; }
	}
}