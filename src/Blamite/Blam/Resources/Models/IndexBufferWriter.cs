using Blamite.IO;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Writes index buffers to a stream.
	/// </summary>
	public static class IndexBufferWriter
	{
		/// <summary>
		///     Writes an index buffer to a stream.
		/// </summary>
		/// <param name="writer">The stream to write to.</param>
		/// <param name="indices">The indices to write.</param>
		public static void WriteIndexBuffer(IWriter writer, ushort[] indices)
		{
			for (int i = 0; i < indices.Length; i++)
				writer.WriteUInt16(indices[i]);
		}
	}
}