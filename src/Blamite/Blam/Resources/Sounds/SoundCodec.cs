using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundCodec
	{
		public int SampleRate { get; set; }

		public int Encoding { get; set; }

		public int Compression { get; set; }

		public override int GetHashCode()
		{
			return (SampleRate << 16) | (Encoding << 8) | Compression;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundCodec) && (GetHashCode() == obj.GetHashCode());
		}
	}
}