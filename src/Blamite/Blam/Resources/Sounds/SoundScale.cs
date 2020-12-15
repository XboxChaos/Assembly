using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundScale
	{
		public float GainMin { get; set; }

		public float GainMax { get; set; }

		public int PitchMin { get; set; }

		public int PitchMax { get; set; }

		public float SkipFractionMin { get; set; }

		public float SkipFractionMax { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + GainMin.GetHashCode();
			result = result * 8171 + GainMax.GetHashCode();
			result = result * 8171 + ((PitchMin << 16) | PitchMax);
			result = result * 8171 + SkipFractionMin.GetHashCode();
			result = result * 8171 + SkipFractionMax.GetHashCode();
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundScale) && (GetHashCode() == obj.GetHashCode());
		}
	}
}