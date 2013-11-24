using Blamite.IO;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Provides methods for reading index buffers.
	/// </summary>
	public static class IndexBufferReader
	{
		/// <summary>
		///     Reads an index buffer from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <param name="count">The number of indices to read.</param>
		/// <returns>The index buffer that was read.</returns>
		public static ushort[] ReadIndexBuffer(IReader reader, int count)
		{
			var indices = new ushort[count];
			for (int i = 0; i < count; i++)
				indices[i] = reader.ReadUInt16();
			return indices;
		}

		/// <summary>
		///     Skips over an index buffer in a stream.
		/// </summary>
		/// <param name="reader">The stream to seek.</param>
		/// <param name="count">The number of indices in the index buffer.</param>
		public static void SkipIndexBuffer(IReader reader, int count)
		{
			reader.Skip(count*2); // 1 ushort per index
		}
	}
}