using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundPermutation
	{
		public StringID Name { get; set; }

		public int EncodedSkipFraction { get; set; }

		public int SampleSize { get; set; }

		public SoundChunk[] Chunks { get; set; }

		public int EncodedGain { get; set; }

		public int EncodedPermutationInfoIndex { get; set; }

		public int[] LayerMarkers { get; set; }

		public int FSBInfo { get; set; }

		public SoundPermutationLanguage[] Languages { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + (int)Name.Value;
			result = result * 8171 + EncodedSkipFraction;
			result = result * 8171 + SampleSize;

			result = result * 8171 + GetChunksHash();

			result = result * 8171 + EncodedGain;
			result = result * 8171 + EncodedPermutationInfoIndex;

			result = result * 8171 + GetMarkersHash();

			result = result * 8171 + FSBInfo;

			result = result * 8171 + GetLanguagesHash();

			return result;
		}

		public int GetChunksHash()
		{
			int result = 7057;
			if (Chunks != null)
			{
				foreach (var chunk in Chunks)
					result = result * 8171 + chunk.GetHashCode();
			}
			return result;
		}

		public int GetMarkersHash()
		{
			int result = 7057;
			if (LayerMarkers != null)
			{
				foreach (int marker in LayerMarkers)
					result = result * 8171 + marker;
			}
			return result;
		}

		public int GetLanguagesHash()
		{
			int result = 7057;
			if (Languages != null)
			{
				foreach (var lang in Languages)
					result = result * 8171 + lang.GetHashCode();
			}
			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundPermutation) && (GetHashCode() == obj.GetHashCode());
		}
	}

	public class SoundPermutationLanguage
	{
		public int LanguageIndex { get; set; }

		public int SampleSize { get; set; }

		public SoundChunk[] Chunks { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + LanguageIndex;
			result = result * 8171 + SampleSize;

			result = result * 8171 + GetChunksHash();

			return result;
		}

		public int GetChunksHash()
		{
			int result = 7057;
			if (Chunks != null)
			{
				foreach (var chunk in Chunks)
					result = result * 8171 + chunk.GetHashCode();
			}
			return result;
		}
	}
}