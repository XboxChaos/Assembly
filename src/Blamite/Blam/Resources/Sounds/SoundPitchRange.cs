using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPitchRange
	{
		public StringID Name { get; set; }

		public SoundPitchRangeParameter Parameter { get; set; }

		public bool HasEncodedData { get; set; }

		public int RequiredPermutationCount { get; set; }

		public SoundPermutation[] Permutations { get; set; }

		public static int EncodeCountsAndIndex(int requiredCount, int count, int firstIndex)
		{
			int encoded = 0;
			encoded |= (requiredCount & 0x3F) << 26;
			encoded |= (count & 0x3F) << 20;
			encoded |= firstIndex & 0xFFFFF;
			return encoded;
		}

		public static void DecodeCountsAndIndex(int encodedCountsAndIndex, out int requiredCount, out int count, out int firstIndex)
		{
			requiredCount = (int)(encodedCountsAndIndex & 0xFC000000) >> 26;
			count = (encodedCountsAndIndex & 0x3F00000) >> 20;
			firstIndex = encodedCountsAndIndex & 0xFFFFF;
		}

		public int GetPermutationsHash()
		{
			int result = 7057;
			if (Permutations != null)
			{
				foreach (var perm in Permutations)
					result = result * 8171 + perm.GetHashCode();
			}
			return result;
		}
	}
}