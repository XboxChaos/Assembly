using System;
using System.Collections.Generic;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Serializes vertex buffer data.
	/// </summary>
	public class VertexBufferWriter
	{
		private readonly Stack<BoundingBox> _bboxStack = new Stack<BoundingBox>();
		private readonly IWriter _writer;
		private BoundingBox _bbox;

		/// <summary>
		///     Initializes a new instance of the <see cref="VertexBufferWriter" /> class.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		public VertexBufferWriter(IWriter writer)
		{
			_writer = writer;
		}

		/// <summary>
		///     Begins using a bounding box to transform vertices.
		///     If a bounding box is already in use, it will be saved and then restored when this bounding box is no longer in use.
		/// </summary>
		/// <param name="bbox">The bounding box.</param>
		public void BeginBoundingBox(BoundingBox bbox)
		{
			_bboxStack.Push(_bbox);
			_bbox = bbox;
		}

		/// <summary>
		///     Stops using the current bounding box.
		/// </summary>
		public void EndLayout()
		{
			_bbox = _bboxStack.Pop();
		}

		/// <summary>
		///     Writes a vertex element to the stream.
		/// </summary>
		/// <param name="x">The X component.</param>
		/// <param name="y">The Y component.</param>
		/// <param name="z">The Z component.</param>
		/// <param name="w">The W component.</param>
		/// <param name="layout">The layout of the element.</param>
		public void WriteElement(float x, float y, float z, float w, VertexElementLayout layout)
		{
			TransformElement(ref x, ref y, ref z, ref w, layout.Usage);

			// This is only enough to support s_rigid_vertex for now
			uint val = 0;
			switch (layout.Type)
			{
				case VertexElementType.UDec4N:
					val = (uint) (x*1023.0f) & 0x3FF;
					val |= ((uint) (y*1023.0f) & 0x3FF) << 10;
					val |= ((uint) (z*1023.0f) & 0x3FF) << 20;
					val |= (uint) (w*3.0f) << 30;
					_writer.WriteUInt32(val);
					break;

				case VertexElementType.UShort2N:
					_writer.WriteUInt16((ushort) (x*65535.0f));
					_writer.WriteUInt16((ushort) (y*65535.0f));
					_writer.WriteUInt16((ushort) (z*65535.0f));
					break;

				case VertexElementType.DHen3N:
					val = (((uint) (x*511.0f) << 22) & 0xFFC00000) >> 22;
					val |= (((uint) (y*1023.0f) << 21) & 0xFFE00000) >> 11;
					val |= ((uint) (z*1023.0f) << 21) & 0xFFE00000;
					_writer.WriteUInt32(val);
					break;

				default:
					throw new NotSupportedException("Unsupported vertex element type: " +
					                                Enum.GetName(typeof (VertexElementType), layout.Type));
			}
		}

		public void WriteOpaqueNode(byte x)
		{
			_writer.WriteByte(x);
		}

		public void WriteOpaqueNode(float x, float y, float z)
		{
			_writer.WriteFloat(x);
			_writer.WriteFloat(y);
			_writer.WriteFloat(z);
		}

		private void TransformElement(ref float x, ref float y, ref float z, ref float w, VertexElementUsage usage)
		{
			if (_bbox == null)
				return;

			switch (usage)
			{
				case VertexElementUsage.Position:
					x = (x - _bbox.MinX)/(_bbox.MaxX - _bbox.MinX);
					y = (y - _bbox.MinY)/(_bbox.MaxY - _bbox.MinY);
					z = (z - _bbox.MinZ)/(_bbox.MaxZ - _bbox.MinZ);
					break;

				case VertexElementUsage.TexCoords:
					x = (x - _bbox.MinU)/(_bbox.MaxU - _bbox.MinU);
					y = (y - _bbox.MinV)/(_bbox.MaxV - _bbox.MinV);
					break;
			}
		}
	}
}