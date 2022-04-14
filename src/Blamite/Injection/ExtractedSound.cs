using Blamite.Blam;
using Blamite.Blam.Resources.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Injection
{
	public class ExtractedSoundCodec
	{
		public int OriginalIndex { get; set; }

		public SoundCodec Source { get; set; }

		public ExtractedSoundCodec(int index, SoundCodec src)
		{
			OriginalIndex = index;
			Source = src;
		}
	}

	public class ExtractedSoundPitchRange
	{
		public int OriginalIndex { get; set; }

		public string Name { get; set; }

		public SoundPitchRangeParameter Parameter { get; set; }

		public bool HasEncodedData { get; set; }
		public int RequiredPermutationCount { get; set; }

		public List<ExtractedSoundPermutation> Permutations { get; set; }

		public ExtractedSoundPitchRange(int index, string name, SoundPitchRange orig, List<ExtractedSoundPermutation> perms)
		{
			OriginalIndex = index;
			Name = name;
			Parameter = orig.Parameter;
			HasEncodedData = orig.HasEncodedData;
			RequiredPermutationCount = orig.RequiredPermutationCount;
			Permutations = perms;
		}

		public ExtractedSoundPitchRange(int index, string name, SoundPitchRangeParameter param, bool hasData, int requiredCount, List<ExtractedSoundPermutation> perms)
		{
			OriginalIndex = index;
			Name = name;
			Parameter = param;
			HasEncodedData = hasData;
			RequiredPermutationCount = requiredCount;
			Permutations = perms;
		}
	}

	public class ExtractedSoundPermutation
	{
		public string Name { get; set; }

		public int EncodedSkipFraction { get; set; }

		public int SampleSize { get; set; }

		public List<ExtractedSoundChunk> Chunks { get; set; }

		public int EncodedGain { get; set; }
		
		public int EncodedPermutationInfoIndex { get; set; }

		public List<ExtractedSoundLanguagePermutation> Languages { get; set; }

		public List<int> LayerMarkers { get; set; }

		public int FSBInfo { get; set; }
	}

	public class ExtractedSoundLanguagePermutation
	{
		public int LanguageIndex { get; set; }

		public int SampleSize { get; set; }

		public List<ExtractedSoundChunk> Chunks { get; set; }
	}

	public class ExtractedSoundChunk
	{
		public SoundChunk Source { get; set; }

		public string FModBankSuffix { get; set; }

		public ExtractedSoundChunk(SoundChunk src, string bankSuffix)
		{
			Source = src;

			FModBankSuffix = bankSuffix == null ? "" : bankSuffix;
		}
	}

	public class ExtractedSoundLanguageDuration
	{
		public int OriginalIndex { get; set; }

		public List<ExtractedSoundLanguageDurationInfo> Languages { get; set; }

		public ExtractedSoundLanguageDuration(int index)
		{
			OriginalIndex = index;
			Languages = new List<ExtractedSoundLanguageDurationInfo>();
		}
	}

	public class ExtractedSoundLanguageDurationInfo
	{
		public int LanguageIndex { get; set; }

		public List<int> Durations { get; set; }
	}

	public class ExtractedSoundPlayback
	{
		public int OriginalIndex { get; set; }

		public SoundPlayback Source { get; set; }

		public ExtractedSoundPlayback(int index, SoundPlayback src)
		{
			OriginalIndex = index;
			Source = src;
		}
	}

	public class ExtractedSoundScale
	{
		public int OriginalIndex { get; set; }

		public SoundScale Source { get; set; }

		public ExtractedSoundScale(int index, SoundScale src)
		{
			OriginalIndex = index;
			Source = src;
		}
	}

	public class ExtractedSoundPromotion
	{
		public int OriginalIndex { get; set; }

		public SoundPromotion Source { get; set; }

		public ExtractedSoundPromotion(int index, SoundPromotion src)
		{
			OriginalIndex = index;

			Source = src;
		}
	}

	public class ExtractedSoundCustomPlayback
	{
		public int OriginalIndex { get; set; }

		public List<SoundCustomPlaybackMix> Mixes { get; set; }

		public int Flags { get; set; }

		public int Unknown { get; set; }
		public int Unknown1 { get; set; }

		public List<SoundCustomPlaybackFilter> Filters { get; set; }

		public List<SoundCustomPlaybackPitchLFO> PitchLFOs { get; set; }

		public List<SoundCustomPlaybackFilterLFO> FilterLFOs { get; set; }

		public int Unknown2 { get; set; }//block
		public int Unknown3 { get; set; }
		public int Unknown4 { get; set; }

		//reach
		public DatumIndex OriginalRadioEffect { get; set; }

		public List<SoundCustomPlaybackLowpassEffect> LowpassEffects { get; set; }

		public List<ExtractedSoundCustomPlaybackComponent> Components { get; set; }

		public SoundCustomPlaybackVersion Version { get; set; }

		public ExtractedSoundCustomPlayback(int index, SoundCustomPlayback src)
		{
			OriginalIndex = index;

			Version = src.Version;

			if (src.Mixes != null)
			{
				Mixes = new List<SoundCustomPlaybackMix>();
				Mixes.AddRange(src.Mixes);
			}

			Flags = src.Flags;

			Unknown = src.Unknown;
			Unknown1 = src.Unknown1;

			if (src.Filters != null)
			{
				Filters = new List<SoundCustomPlaybackFilter>();
				Filters.AddRange(src.Filters);
			}

			if (src.PitchLFOs != null)
			{
				PitchLFOs = new List<SoundCustomPlaybackPitchLFO>();
				PitchLFOs.AddRange(src.PitchLFOs);
			}

			if (src.FilterLFOs != null)
			{
				FilterLFOs = new List<SoundCustomPlaybackFilterLFO>();
				FilterLFOs.AddRange(src.FilterLFOs);
			}

			Unknown2 = src.Unknown2;
			Unknown3 = src.Unknown3;
			Unknown4 = src.Unknown4;

			OriginalRadioEffect = src.RadioEffect != null ? src.RadioEffect.Index : DatumIndex.Null;

			if (src.LowpassEffects != null)
			{
				LowpassEffects = new List<SoundCustomPlaybackLowpassEffect>();
				LowpassEffects.AddRange(src.LowpassEffects);
			}

			if (src.Components != null)
			{
				Components = new List<ExtractedSoundCustomPlaybackComponent>();
				foreach (var comp in src.Components)
				{
					Components.Add(new ExtractedSoundCustomPlaybackComponent(comp));
				}
			}
		}

		public ExtractedSoundCustomPlayback() { }

		public override int GetHashCode()
		{
			int result = 7057;

			for (int i = 0; i < Mixes.Count; i++)
				result = result * 8171 + Mixes[i].GetHashCode();

			result = result * 8171 + Flags;
			result = result * 8171 + Unknown;
			result = result * 8171 + Unknown1;

			for (int i = 0; i < Filters.Count; i++)
				result = result * 8171 + Filters[i].GetHashCode();

			for (int i = 0; i < PitchLFOs.Count; i++)
				result = result * 8171 + PitchLFOs[i].GetHashCode();

			for (int i = 0; i < FilterLFOs.Count; i++)
				result = result * 8171 + FilterLFOs[i].GetHashCode();

			result = result * 8171 + Unknown2;
			result = result * 8171 + Unknown3;
			result = result * 8171 + Unknown4;

			result = result * 8171 + (int)OriginalRadioEffect.Value;

			for (int i = 0; i < LowpassEffects.Count; i++)
				result = result * 8171 + LowpassEffects[i].GetHashCode();

			for (int i = 0; i < Components.Count; i++)
				result = result * 8171 + Components[i].GetHashCode();

			return result;
		}
	}

	public class ExtractedSoundCustomPlaybackComponent
	{
		public DatumIndex OriginalSound { get; set; }

		public float Gain { get; set; }

		public int Flags { get; set; }

		public ExtractedSoundCustomPlaybackComponent(SoundCustomPlaybackComponent src)
		{
			OriginalSound = src.Sound != null ? src.Sound.Index : DatumIndex.Null;

			Gain = src.Gain;

			Flags = src.Flags;
		}

		public ExtractedSoundCustomPlaybackComponent() { }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + (int)OriginalSound.Value;
			result = result * 8171 + Gain.GetHashCode();
			result = result * 8171 + Flags;
			return result;
		}
	}

	public class ExtractedSoundExtraInfo
	{
		public int OriginalIndex { get; set; }

		public SoundExtraInfo Source { get; set; }

		public ExtractedSoundExtraInfo(int index, SoundExtraInfo src)
		{
			OriginalIndex = index;
			Source = src;
		}
	}
}
