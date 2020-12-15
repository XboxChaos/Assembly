using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPromotion
	{
		public SoundPromotionRule[] Rules { get; set; }

		public int ActivePromotionIndex { get; set; }

		public int LastPromotionTime { get; set; }

		public int SuppressionTimeout { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + ActivePromotionIndex;
			result = result * 8171 + LastPromotionTime;
			result = result * 8171 + SuppressionTimeout;
			
			for (int i = 0; i < Rules.Length; i++)
				result = result * 8171 + Rules[i].GetHashCode();

			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundPromotion) && (GetHashCode() == obj.GetHashCode());
		}
	}

	public class SoundPromotionRule
	{
		public int LocalPitchRangeIndex { get; set; }

		public int MaximumPlayCount { get; set; }

		public float SupressionTime { get; set; }

		public int RolloverTime { get; set; }

		public int ImpulseTime { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + LocalPitchRangeIndex;
			result = result * 8171 + MaximumPlayCount;
			result = result * 8171 + SupressionTime.GetHashCode();
			result = result * 8171 + RolloverTime;
			result = result * 8171 + ImpulseTime;
			return result;
		}
	}
}