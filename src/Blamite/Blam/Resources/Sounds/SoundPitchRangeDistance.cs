using Blamite.Serialization;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPitchRangeDistance
	{
		public float DontObstructDistance { get; set; }

		public float DontPlayDistance { get; set; }

		public float AttackDistance { get; set; }

		public float MinDistance { get; set; }

		public float SustainBeginDistance { get; set; }

		public float SustainEndDistance { get; set; }

		public float MaxDistance { get; set; }

		public float SustainDB { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + DontObstructDistance.GetHashCode();
			result = result * 8171 + DontPlayDistance.GetHashCode();
			result = result * 8171 + AttackDistance.GetHashCode();
			result = result * 8171 + MinDistance.GetHashCode();
			result = result * 8171 + SustainBeginDistance.GetHashCode();
			result = result * 8171 + SustainEndDistance.GetHashCode();
			result = result * 8171 + MaxDistance.GetHashCode();
			result = result * 8171 + SustainDB.GetHashCode();
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundPitchRangeDistance) && (GetHashCode() == obj.GetHashCode());
		}
	}
}
