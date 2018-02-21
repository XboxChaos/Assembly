﻿using Blamite.Blam.Resources.Sounds;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Sounds
{
	public class FourthGenSoundScale : ISoundScale
	{
		public FourthGenSoundScale(StructureValueCollection values)
		{
			Load(values);
		}

		public int GainMin { get; private set; }

		public int GainMax { get; private set; }

		public short PitchMin { get; private set; }

		public short PitchMax { get; private set; }

		public int SkipFractionMin { get; private set; }

		public int SkipFractionMax { get; private set; }

		private void Load(StructureValueCollection values)
		{
			GainMin = (int)values.GetInteger("gain min");
			GainMax = (int)values.GetInteger("gain max");
			PitchMin = (short)values.GetInteger("pitch min");
			PitchMax = (short)values.GetInteger("pitch max");
			SkipFractionMin = (int)values.GetInteger("skip fraction min");
			SkipFractionMax = (int)values.GetInteger("skip fraction max");
		}
	}
}
