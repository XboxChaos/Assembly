using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPitchRangeParameter
	{
		public int NaturalPitch { get; set; }

		public SoundPitchRangeDistance Distance { get; set; }

		public int BendMin { get; set; }

		public int BendMax { get; set; }

		public int MaxGainPitchMin { get; set; }

		public int MaxGainPitchMax { get; set; }

		public int PlaybackPitchMin { get; set; }

		public int PlaybackPitchMax { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + NaturalPitch;
			if (Distance != null)
				result = result * 8171 + Distance.GetHashCode();
			result = result * 8171 + ((BendMin << 16) | BendMax);
			result = result * 8171 + ((MaxGainPitchMin << 16) | MaxGainPitchMax);
			result = result * 8171 + ((PlaybackPitchMin << 16) | PlaybackPitchMax);
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundPitchRangeParameter) && (GetHashCode() == obj.GetHashCode());
		}
	}
}