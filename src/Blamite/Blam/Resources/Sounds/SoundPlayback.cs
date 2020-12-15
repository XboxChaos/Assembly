using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPlayback
	{
		public int InternalFlags { get; set; }

		public float DontObstructDistance { get; set; }

		public float DontPlayDistance { get; set; }

		public float AttackDistance { get; set; }

		public float MinDistance { get; set; }

		public float SustainBeginDistance { get; set; }

		public float SustainEndDistance { get; set; }

		public float MaxDistance { get; set; }

		public float SustainDB { get; set; }

		public float SkipFraction { get; set; }

		public float MaxPendPerSec { get; set; }

		public float GainBase { get; set; }

		public float GainVariance { get; set; }

		public int RandomPitchBoundsMin { get; set; }

		public int RandomPitchBoundsMax { get; set; }

		public float InnerConeAngle { get; set; }

		public float OuterConeAngle { get; set; }

		public float OuterConeGain { get; set; }

		public int Flags { get; set; }

		public float Azimuth { get; set; }

		public float PositionalGain { get; set; }

		public float FirstPersonGain { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + InternalFlags;
			result = result * 8171 + DontObstructDistance.GetHashCode();
			result = result * 8171 + DontPlayDistance.GetHashCode();
			result = result * 8171 + AttackDistance.GetHashCode();
			result = result * 8171 + MinDistance.GetHashCode();
			result = result * 8171 + SustainBeginDistance.GetHashCode();
			result = result * 8171 + SustainEndDistance.GetHashCode();
			result = result * 8171 + MaxDistance.GetHashCode();
			result = result * 8171 + SustainDB.GetHashCode();
			result = result * 8171 + SkipFraction.GetHashCode();
			result = result * 8171 + MaxPendPerSec.GetHashCode();
			result = result * 8171 + GainBase.GetHashCode();
			result = result * 8171 + GainVariance.GetHashCode();
			result = result * 8171 + RandomPitchBoundsMin;
			result = result * 8171 + RandomPitchBoundsMax;
			result = result * 8171 + InnerConeAngle.GetHashCode();
			result = result * 8171 + OuterConeAngle.GetHashCode();
			result = result * 8171 + OuterConeGain.GetHashCode();
			result = result * 8171 + Flags;
			result = result * 8171 + Azimuth.GetHashCode();
			result = result * 8171 + PositionalGain.GetHashCode();
			result = result * 8171 + FirstPersonGain.GetHashCode();
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundPlayback) && (GetHashCode() == obj.GetHashCode());
		}
	}
}
