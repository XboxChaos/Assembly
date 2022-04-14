using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundChunk
	{
		public int FileOffset { get; set; }

		public int EncodedSizeAndFlags { get; set; }

		public int CacheIndex { get; set; }

		public int XMA2BufferStart { get; set; }

		public int XMA2BufferEnd { get; set; }

		public int Unknown { get; set; }

		public int Unknown1 { get; set; }

		public StringID FModBankSuffix { get; set; }

		/// <summary>
		/// Get or set the chunk size inside <see cref="EncodedSizeAndFlags"/>.
		/// </summary>
		public int DecodedSize
		{
			get { return EncodedSizeAndFlags & 0xFFFFFF; }
		}

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + FileOffset;
			result = result * 8171 + EncodedSizeAndFlags;
			result = result * 8171 + CacheIndex;
			result = result * 8171 + XMA2BufferStart;
			result = result * 8171 + XMA2BufferEnd;
			result = result * 8171 + Unknown;
			result = result * 8171 + Unknown1;
			result = result * 8171 + (int)FModBankSuffix.Value;
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundChunk) && (GetHashCode() == obj.GetHashCode());
		}
	}
}