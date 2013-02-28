using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.Resources.Models
{
    /// <summary>
    /// Provides methods for reading vertex buffers.
    /// </summary>
    public static class VertexBufferReader
    {
        /// <summary>
        /// Reads vertices from a vertex buffer.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <param name="layout">The layout of each vertex to read.</param>
        /// <param name="count">The number of vertices to read.</param>
        /// <param name="processor">The IVertexProcessor to send read vertices to. Can be null.</param>
        public static void ReadVertices(IReader reader, VertexLayout layout, int count, IVertexProcessor processor)
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
                        ReadElement(reader, element, processor);
                    }
                }

                if (processor != null)
                    processor.EndVertex();
            }
        }

        /// <summary>
        /// Skips over the extra per-vertex elements at the end of a vertex buffer.
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
                    reader.Skip((totalVertices * section.ExtraElementsPerVertex + 3) & ~3);
                    break;
                case ExtraVertexElementType.Float3:
                    reader.Skip(totalVertices * section.ExtraElementsPerVertex * 3);
                    break;
                case ExtraVertexElementType.None:
                    break;
                default:
                    throw new InvalidOperationException("Unsupported extra vertex element type: " + section.ExtraElementsType);
            }
        }

        /// <summary>
        /// Reads a vertex element from a stream and sends it to an IVertexProcessor.
        /// </summary>
        /// <param name="reader">The stream to read from. It should be positioned at the start of the element.</param>
        /// <param name="element">The layout of the element to read.</param>
        /// <param name="processor">The IVertexProcessor to send the element to.</param>
        private static void ReadElement(IReader reader, VertexElementLayout element, IVertexProcessor processor)
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
                    x = reader.ReadByte() / 255.0f;
                    y = reader.ReadByte() / 255.0f;
                    z = reader.ReadByte() / 255.0f;
                    w = reader.ReadByte() / 255.0f;
                    break;

                case VertexElementType.Byte4N:
                    x = reader.ReadSByte() / 127.0f;
                    y = reader.ReadSByte() / 127.0f;
                    z = reader.ReadSByte() / 127.0f;
                    w = reader.ReadSByte() / 127.0f;
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
                    x = reader.ReadUInt16() / 65535.0f;
                    y = reader.ReadUInt16() / 65535.0f;
                    break;

                case VertexElementType.UShort4N:
                    x = reader.ReadUInt16() / 65535.0f;
                    y = reader.ReadUInt16() / 65535.0f;
                    z = reader.ReadUInt16() / 65535.0f;
                    w = reader.ReadUInt16() / 65535.0f;
                    break;

                case VertexElementType.Short4N:
                    x = reader.ReadInt16() / 32767.0f;
                    y = reader.ReadInt16() / 32767.0f;
                    z = reader.ReadInt16() / 32767.0f;
                    w = reader.ReadInt16() / 32767.0f;
                    break;

                case VertexElementType.D3DColor:
                    w = reader.ReadByte() / 255.0f; // W is set here because alpha comes first but ends up last in the vector
                    y = reader.ReadByte() / 255.0f;
                    z = reader.ReadByte() / 255.0f;
                    x = reader.ReadByte() / 255.0f;
                    break;

                case VertexElementType.DHen3N:
                    value = reader.ReadUInt32();
                    x = (((int)((value << 21) & 0xFFE00000)) >> 21) / 1023.0f;
                    y = (((int)((value << 10) & 0xFFE00000)) >> 21) / 1023.0f;
                    z = (((int)(value & 0xFFC00000)) >> 22) / 511.0f;
                    break;

                case VertexElementType.UDec4N:
                    value = reader.ReadUInt32();
                    x = (value & 0x3FF) / 1023.0f;
                    y = ((value >> 10) & 0x3FF) / 1023.0f;
                    z = ((value >> 20) & 0x3FF) / 1023.0f;
                    w = (value >> 30) / 3.0f;
                    break;

                default:
                    throw new NotSupportedException("Unsupported vertex element type: " + Enum.GetName(typeof(VertexElementType), element.Type));
            }

            if (processor != null)
                processor.ProcessVertexElement(x, y, z, w, element);
        }
    }
}
