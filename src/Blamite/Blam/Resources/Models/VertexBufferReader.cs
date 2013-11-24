using System;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Provides methods for reading vertex buffers.
	/// </summary>
	public static class VertexBufferReader
	{
		/// <summary>
		///     Reads vertices from a vertex buffer.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="layout">The layout of each vertex to read.</param>
		/// <param name="count">The number of vertices to read.</param>
		/// <param name="boundingBox">The bounding box to transform vertices with. Can be null.</param>
		/// <param name="processor">The IVertexProcessor to send read vertices to. Can be null.</param>
		public static void ReadVertices(IReader reader, VertexLayout layout, int count, BoundingBox boundingBox,
			IVertexProcessor processor)
		{
			for (int i = 0; i < count; i++)
			{
				long vertexStartPos = reader.Position;
				if (processor != null)
					processor.BeginVertex();

				foreach (VertexElementLayout element in layout.Elements)
				{
					if (element.Stream == 0) // Not sure how multistream vertices work yet
					{
						reader.SeekTo(vertexStartPos + element.Offset);
						ReadElement(reader, element, boundingBox, processor);
					}
				}

				if (processor != null)
					processor.EndVertex();
			}
		}

		/// <summary>
		///     Skips over the extra per-vertex elements at the end of a vertex buffer.
		/// </summary>
		/// <param name="reader">The stream to seek.</param>
		/// <param name="section">The current model section.</param>
		public static void SkipExtraElements(IReader reader, IModelSection section)
		{
			int totalVertices = 0;
			foreach (IModelSubmesh submesh in section.Submeshes)
				totalVertices += submesh.VertexBufferCount;

			switch (section.ExtraElementsType)
			{
				case ExtraVertexElementType.Byte:
					reader.Skip((totalVertices*section.ExtraElementsPerVertex + 3) & ~3);
					break;
				case ExtraVertexElementType.Float3:
					reader.Skip(totalVertices*section.ExtraElementsPerVertex*3);
					break;
				case ExtraVertexElementType.None:
					break;
				default:
					throw new InvalidOperationException("Unsupported extra vertex element type: " + section.ExtraElementsType);
			}
		}

		/// <summary>
		///     Reads a vertex element from a stream and sends it to an IVertexProcessor.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the start of the element.</param>
		/// <param name="element">The layout of the element to read.</param>
		/// <param name="boundingBox">The bounding box to transform the element with. Can be null.</param>
		/// <param name="processor">The IVertexProcessor to send the element to.</param>
		private static void ReadElement(IReader reader, VertexElementLayout element, BoundingBox boundingBox,
			IVertexProcessor processor)
		{
			// EW EW EW
			// TODO: Implement everything, this is just enough to load some Reach vertices for now...
			float x = 0, y = 0, z = 0, w = 1;
			uint value;
			switch (element.Type)
			{
				case VertexElementType.Float2:
					x = reader.ReadFloat();
					y = reader.ReadFloat();
					break;

				case VertexElementType.Float3:
					x = reader.ReadFloat();
					y = reader.ReadFloat();
					z = reader.ReadFloat();
					break;

				case VertexElementType.Float4:
					x = reader.ReadFloat();
					y = reader.ReadFloat();
					z = reader.ReadFloat();
					w = reader.ReadFloat();
					break;

				case VertexElementType.UByte4:
					x = reader.ReadByte();
					y = reader.ReadByte();
					z = reader.ReadByte();
					w = reader.ReadByte();
					break;

				case VertexElementType.UByte4N:
					x = reader.ReadByte()/255.0f;
					y = reader.ReadByte()/255.0f;
					z = reader.ReadByte()/255.0f;
					w = reader.ReadByte()/255.0f;
					break;

				case VertexElementType.Byte4N:
					x = reader.ReadSByte()/127.0f;
					y = reader.ReadSByte()/127.0f;
					z = reader.ReadSByte()/127.0f;
					w = reader.ReadSByte()/127.0f;
					break;

				case VertexElementType.Float16_2:
					x = Half.ToHalf(reader.ReadUInt16());
					y = Half.ToHalf(reader.ReadUInt16());
					break;

				case VertexElementType.Float16_4:
					x = Half.ToHalf(reader.ReadUInt16());
					y = Half.ToHalf(reader.ReadUInt16());
					z = Half.ToHalf(reader.ReadUInt16());
					w = Half.ToHalf(reader.ReadUInt16());
					break;

				case VertexElementType.UShort2:
					x = reader.ReadUInt16();
					y = reader.ReadUInt16();
					break;

				case VertexElementType.UShort2N:
					x = reader.ReadUInt16()/65535.0f;
					y = reader.ReadUInt16()/65535.0f;
					break;

				case VertexElementType.UShort4N:
					x = reader.ReadUInt16()/65535.0f;
					y = reader.ReadUInt16()/65535.0f;
					z = reader.ReadUInt16()/65535.0f;
					w = reader.ReadUInt16()/65535.0f;
					break;

				case VertexElementType.Short4N:
					x = reader.ReadInt16()/32767.0f;
					y = reader.ReadInt16()/32767.0f;
					z = reader.ReadInt16()/32767.0f;
					w = reader.ReadInt16()/32767.0f;
					break;

				case VertexElementType.D3DColor:
					w = reader.ReadByte()/255.0f; // W is set here because alpha comes first but ends up last in the vector
					y = reader.ReadByte()/255.0f;
					z = reader.ReadByte()/255.0f;
					x = reader.ReadByte()/255.0f;
					break;

				case VertexElementType.DHen3N:
					value = reader.ReadUInt32();
					x = (((int) ((value << 22) & 0xFFC00000)) >> 22)/511.0f;
					y = (((int) ((value << 11) & 0xFFE00000)) >> 21)/1023.0f;
					z = (((int) (value & 0xFFE00000)) >> 21)/1023.0f;
					break;

				case VertexElementType.UDec4N:
					value = reader.ReadUInt32();
					x = (value & 0x3FF)/1023.0f;
					y = ((value >> 10) & 0x3FF)/1023.0f;
					z = ((value >> 20) & 0x3FF)/1023.0f;
					w = (value >> 30)/3.0f;
					break;

				default:
					throw new NotSupportedException("Unsupported vertex element type: " +
					                                Enum.GetName(typeof (VertexElementType), element.Type));
			}

			if (boundingBox != null)
				TransformElement(ref x, ref y, ref z, ref w, element.Usage, boundingBox);

			if (processor != null)
				processor.ProcessVertexElement(x, y, z, w, element);
		}

		/// <summary>
		///     Transforms a vertex element based upon a model's bounding box information.
		/// </summary>
		/// <param name="x">The X component of the element.</param>
		/// <param name="y">The Y component of the element.</param>
		/// <param name="z">The Z component of the element.</param>
		/// <param name="w">The W component of the element.</param>
		/// <param name="usage">The usage of the vertex element.</param>
		/// <param name="boundingBox">The bounding box to transform the element by.</param>
		private static void TransformElement(ref float x, ref float y, ref float z, ref float w, VertexElementUsage usage,
			BoundingBox boundingBox)
		{
			switch (usage)
			{
				case VertexElementUsage.Position:
					x = x*(boundingBox.MaxX - boundingBox.MinX) + boundingBox.MinX;
					y = y*(boundingBox.MaxY - boundingBox.MinY) + boundingBox.MinY;
					z = z*(boundingBox.MaxZ - boundingBox.MinZ) + boundingBox.MinZ;
					break;

				case VertexElementUsage.TexCoords:
					x = x*(boundingBox.MaxU - boundingBox.MinU) + boundingBox.MinU;
					y = y*(boundingBox.MaxV - boundingBox.MinV) + boundingBox.MinV;
					break;
			}
		}
	}
}