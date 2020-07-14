using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public enum SoundCustomPlaybackVersion
	{
		Default,
		Reach
	}

	public class SoundCustomPlayback
	{
		public SoundCustomPlaybackMix[] Mixes { get; set; }

		public int Flags { get; set; }

		public int Unknown { get; set; }
		public int Unknown1 { get; set; }

		public SoundCustomPlaybackFilter[] Filters { get; set; }

		public SoundCustomPlaybackPitchLFO[] PitchLFOs { get; set; }

		public SoundCustomPlaybackFilterLFO[] FilterLFOs { get; set; }

		public int Unknown2 { get; set; }//block
		public int Unknown3 { get; set; }
		public int Unknown4 { get; set; }

		public SoundCustomPlaybackVersion Version { get; set; }

		//reach
		public ITag RadioEffect { get; set; }

		public SoundCustomPlaybackLowpassEffect[] LowpassEffects { get; set; }

		public SoundCustomPlaybackComponent[] Components { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;

			for (int i = 0; i < Mixes.Length; i++)
				result = result * 8171 + Mixes[i].GetHashCode();

			result = result * 8171 + Flags;
			result = result * 8171 + Unknown;
			result = result * 8171 + Unknown1;

			for (int i = 0; i < Filters.Length; i++)
				result = result * 8171 + Filters[i].GetHashCode();

			for (int i = 0; i < PitchLFOs.Length; i++)
				result = result * 8171 + PitchLFOs[i].GetHashCode();

			for (int i = 0; i < FilterLFOs.Length; i++)
				result = result * 8171 + FilterLFOs[i].GetHashCode();

			result = result * 8171 + Unknown2;
			result = result * 8171 + Unknown3;
			result = result * 8171 + Unknown4;

			result = result * 8171 + (RadioEffect != null ? (int)RadioEffect.Index.Value : 0);

			for (int i = 0; i < LowpassEffects.Length; i++)
				result = result * 8171 + LowpassEffects[i].GetHashCode();

			for (int i = 0; i < Components.Length; i++)
				result = result * 8171 + Components[i].GetHashCode();

			return result;
		}

		public override bool Equals(object obj)
		{
			return (obj is SoundCustomPlayback) && (GetHashCode() == obj.GetHashCode());
		}
	}

	public class SoundCustomPlaybackLowpassEffect
	{
		public float Attack { get; set; }
		public float Release { get; set; }
		public float CutoffFrequency { get; set; }
		public float OutputGain { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + Attack.GetHashCode();
			result = result * 8171 + Release.GetHashCode();
			result = result * 8171 + CutoffFrequency.GetHashCode();
			result = result * 8171 + OutputGain.GetHashCode();
			return result;
		}
	}

	public class SoundCustomPlaybackComponent
	{
		public ITag Sound { get; set; }

		public float Gain { get; set; }

		public int Flags { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + (Sound != null ? (int)Sound.Index.Value : 0);
			result = result * 8171 + Gain.GetHashCode();
			result = result * 8171 + Flags;
			return result;
		}
	}

	public class SoundCustomPlaybackMix
	{
		public int Mixbin { get; set; }

		public float Gain { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + Mixbin;
			result = result * 8171 + Gain.GetHashCode();
			return result;
		}
	}

	public class SoundCustomPlaybackFilter
	{
		public int Type { get; set; }
		public int Width { get; set; }

		public float LeftFreqScaleMin { get; set; }
		public float LeftFreqScaleMax { get; set; }
		public float LeftFreqRandomBase { get; set; }
		public float LeftFreqRandomVariance { get; set; }

		public float LeftGainScaleMin { get; set; }
		public float LeftGainScaleMax { get; set; }
		public float LeftGainRandomBase { get; set; }
		public float LeftGainRandomVariance { get; set; }

		public float RightFreqScaleMin { get; set; }
		public float RightFreqScaleMax { get; set; }
		public float RightFreqRandomBase { get; set; }
		public float RightFreqRandomVariance { get; set; }

		public float RightGainScaleMin { get; set; }
		public float RightGainScaleMax { get; set; }
		public float RightGainRandomBase { get; set; }
		public float RightGainRandomVariance { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + LeftFreqScaleMin.GetHashCode();
			result = result * 8171 + LeftFreqScaleMax.GetHashCode();
			result = result * 8171 + LeftFreqRandomBase.GetHashCode();
			result = result * 8171 + LeftFreqRandomVariance.GetHashCode();

			result = result * 8171 + LeftGainScaleMin.GetHashCode();
			result = result * 8171 + LeftGainScaleMax.GetHashCode();
			result = result * 8171 + LeftGainRandomBase.GetHashCode();
			result = result * 8171 + LeftGainRandomVariance.GetHashCode();

			result = result * 8171 + RightFreqScaleMin.GetHashCode();
			result = result * 8171 + RightFreqScaleMax.GetHashCode();
			result = result * 8171 + RightFreqRandomBase.GetHashCode();
			result = result * 8171 + RightFreqRandomVariance.GetHashCode();

			result = result * 8171 + RightGainScaleMin.GetHashCode();
			result = result * 8171 + RightGainScaleMax.GetHashCode();
			result = result * 8171 + RightGainRandomBase.GetHashCode();
			result = result * 8171 + RightGainRandomVariance.GetHashCode();
			return result;
		}
	}

	public class SoundCustomPlaybackPitchLFO
	{
		public float DelayScaleMin { get; set; }
		public float DelayScaleMax { get; set; }
		public float DelayRandomBase { get; set; }
		public float DelayRandomVariance { get; set; }

		public float FreqScaleMin { get; set; }
		public float FreqScaleMax { get; set; }
		public float FreqRandomBase { get; set; }
		public float FreqRandomVariance { get; set; }

		public float PitchModScaleMin { get; set; }
		public float PitchModScaleMax { get; set; }
		public float PitchModRandomBase { get; set; }
		public float PitchModRandomVariance { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + DelayScaleMin.GetHashCode();
			result = result * 8171 + DelayScaleMax.GetHashCode();
			result = result * 8171 + DelayRandomBase.GetHashCode();
			result = result * 8171 + DelayRandomVariance.GetHashCode();

			result = result * 8171 + FreqScaleMin.GetHashCode();
			result = result * 8171 + FreqScaleMax.GetHashCode();
			result = result * 8171 + FreqRandomBase.GetHashCode();
			result = result * 8171 + FreqRandomVariance.GetHashCode();

			result = result * 8171 + PitchModScaleMin.GetHashCode();
			result = result * 8171 + PitchModScaleMax.GetHashCode();
			result = result * 8171 + PitchModRandomBase.GetHashCode();
			result = result * 8171 + PitchModRandomVariance.GetHashCode();
			return result;
		}
	}

	public class SoundCustomPlaybackFilterLFO
	{
		public float DelayScaleMin { get; set; }
		public float DelayScaleMax { get; set; }
		public float DelayRandomBase { get; set; }
		public float DelayRandomVariance { get; set; }

		public float FreqScaleMin { get; set; }
		public float FreqScaleMax { get; set; }
		public float FreqRandomBase { get; set; }
		public float FreqRandomVariance { get; set; }

		public float CutoffModScaleMin { get; set; }
		public float CutoffModScaleMax { get; set; }
		public float CutoffModRandomBase { get; set; }
		public float CutoffModRandomVariance { get; set; }

		public float GainModScaleMin { get; set; }
		public float GainModScaleMax { get; set; }
		public float GainModRandomBase { get; set; }
		public float GainModRandomVariance { get; set; }

		public override int GetHashCode()
		{
			int result = 7057;
			result = result * 8171 + DelayScaleMin.GetHashCode();
			result = result * 8171 + DelayScaleMax.GetHashCode();
			result = result * 8171 + DelayRandomBase.GetHashCode();
			result = result * 8171 + DelayRandomVariance.GetHashCode();

			result = result * 8171 + FreqScaleMin.GetHashCode();
			result = result * 8171 + FreqScaleMax.GetHashCode();
			result = result * 8171 + FreqRandomBase.GetHashCode();
			result = result * 8171 + FreqRandomVariance.GetHashCode();

			result = result * 8171 + CutoffModScaleMin.GetHashCode();
			result = result * 8171 + CutoffModScaleMax.GetHashCode();
			result = result * 8171 + CutoffModRandomBase.GetHashCode();
			result = result * 8171 + CutoffModRandomVariance.GetHashCode();

			result = result * 8171 + GainModScaleMin.GetHashCode();
			result = result * 8171 + GainModScaleMax.GetHashCode();
			result = result * 8171 + GainModRandomBase.GetHashCode();
			result = result * 8171 + GainModRandomVariance.GetHashCode();
			return result;
		}
	}
}