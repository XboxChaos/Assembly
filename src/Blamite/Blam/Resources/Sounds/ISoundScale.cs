using System;

namespace Blamite.Blam.Resources.Sounds
{
	public interface ISoundScale
	{
		Int32 GainMin { get; }

		Int32 GainMax { get; }

		Int16 PitchMin { get; }

		Int16 PitchMax { get; }

		Int32 SkipFractionMin { get; }

		Int32 SkipFractionMax { get; }
	}
}
