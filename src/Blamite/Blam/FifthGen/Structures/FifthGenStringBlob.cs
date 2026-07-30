using System.Collections.Generic;
using Blamite.IO;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The NUL-separated name blob a fifth-generation tag layout addresses its names through.
	/// </summary>
	/// <remarks>
	///     Every name in a tag layout - field names, type names, struct names, enumeration option names - is a byte offset into
	///     this blob rather than an index into a list, so two names cannot be assumed to be the same length and an offset
	///     cannot be turned into a record number. An offset which lands on a NUL is an empty name, which is how unnamed
	///     padding fields are spelled. The blob is byte-packed: its chunk is followed immediately by the next one with no
	///     alignment, which is why the chunk walk never rounds a size up.
	/// </remarks>
	public class FifthGenStringBlob
	{
		private readonly Dictionary<uint, string> _cache = new Dictionary<uint, string>();
		private readonly long _contentOffset;

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenStringBlob" /> class.
		/// </summary>
		/// <param name="reader">The stream the blob lives in.</param>
		/// <param name="chunk">The blob's chunk.</param>
		public FifthGenStringBlob(IReader reader, FifthGenChunk chunk)
		{
			_contentOffset = chunk.ContentOffset;
			Size = chunk.Size;
			RawData = chunk.ReadContent(reader);
		}

		/// <summary>
		///     Gets the size of the blob in bytes.
		/// </summary>
		public uint Size { get; private set; }

		/// <summary>
		///     Gets the blob's raw bytes, preserved so that a caller can re-emit them unchanged.
		/// </summary>
		public byte[] RawData { get; private set; }

		/// <summary>
		///     Reads the name at a byte offset into the blob.
		/// </summary>
		/// <param name="reader">The stream the blob lives in. Its position is not preserved.</param>
		/// <param name="offset">The byte offset of the name.</param>
		/// <param name="warnings">A collection to record an out-of-range offset in. Can be null.</param>
		/// <returns>The name, or an empty string if the offset lands on a NUL or falls outside the blob.</returns>
		public string GetString(IReader reader, uint offset, ICollection<string> warnings)
		{
			string cached;
			if (_cache.TryGetValue(offset, out cached))
				return cached;

			if (offset >= Size)
			{
				if (warnings != null)
					warnings.Add($"Name offset 0x{offset:X} falls outside the {Size} byte name blob; treating it as empty.");
				_cache[offset] = string.Empty;
				return string.Empty;
			}

			reader.SeekTo(_contentOffset + offset);
			string result = reader.ReadUTF8((int) (Size - offset));
			_cache[offset] = result;
			return result;
		}
	}
}
