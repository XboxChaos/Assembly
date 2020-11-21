using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundLanguageDuration
	{
		public int LanguageIndex { get; set; }

		public List<SoundLanguagePitchRange> PitchRanges { get; set; }

		public SoundLanguageDuration()
		{
			PitchRanges = new List<SoundLanguagePitchRange>();
		}
	}

	public class SoundLanguagePitchRange
	{
		public int[] Durations { get; set; }
	}
}