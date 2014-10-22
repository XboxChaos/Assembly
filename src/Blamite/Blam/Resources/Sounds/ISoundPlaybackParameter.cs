using System;

namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundPlaybackParameter
	{
		Int16 Unknown { get; }

		Int16 Unknown1 { get; }

		Int32 MinimumDistance { get; }

		Int32 Distance2 { get; }

		Int32 Distance3 { get; }

		Int32 MaximumDistance { get; }

		Int32 Unknown4 { get; }

		Int32 Unknown5 { get; }

		Int32 GainBase { get; }

		Int32 GainVariance { get; }

		Int16 RandomPitchBoundsMin { get; }

		Int16 RandomPitchBoundsMax { get; }

		Int32 InnerConeAngle { get; }

		Int32 OuterConeAngle { get; }

		Int32 OuterConeGain { get; }

		Int32 Flags { get; }

		Int32 Azimuth { get; }

		Int32 PositionalGain { get; }

		Int32 FirstPersonGain { get; }
	}
}
