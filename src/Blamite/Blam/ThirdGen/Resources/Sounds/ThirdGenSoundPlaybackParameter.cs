using Blamite.Blam.Resources.Sounds;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	public class ThirdGenSoundPlaybackParameter : ISoundPlaybackParameter
	{
		public ThirdGenSoundPlaybackParameter(StructureValueCollection values)
		{
			Load(values);
		}

		public short Unknown { get; private set; }

		public short Unknown1 { get; private set; }

		public int MinimumDistance { get; private set; }

		public int Distance2 { get; private set; }

		public int Distance3 { get; private set; }

		public int MaximumDistance { get; private set; }

		public int Unknown4 { get; private set; }

		public int Unknown5 { get; private set; }

		public int GainBase { get; private set; }

		public int GainVariance { get; private set; }

		public short RandomPitchBoundsMin { get; private set; }

		public short RandomPitchBoundsMax { get; private set; }

		public int InnerConeAngle { get; private set; }

		public int OuterConeAngle { get; private set; }

		public int OuterConeGain { get; private set; }

		public int Flags { get; private set; }

		public int Azimuth { get; private set; }

		public int PositionalGain { get; private set; }

		public int FirstPersonGain { get; private set; }

		
		private void Load(StructureValueCollection values)
		{
			Unknown = (short)values.GetInteger("unknown");
			Unknown1 = (short)values.GetInteger("unknown 1");
			MinimumDistance = (int)values.GetInteger("minimum distance");
			Distance2 = (int)values.GetInteger("distance 2");
			Distance3 = (int)values.GetInteger("distance 3");
			MaximumDistance = (int)values.GetInteger("maximum distance");
			Unknown4 = (int)values.GetInteger("unknown 4");
			Unknown5 = (int)values.GetInteger("unknown 5");
			GainBase = (int)values.GetInteger("gain base");
			GainVariance = (int)values.GetInteger("gain variance");
			RandomPitchBoundsMin = (short)values.GetInteger("random pitch bounds min");
			RandomPitchBoundsMax = (short)values.GetInteger("random pitch bounds max");
			InnerConeAngle = (int)values.GetInteger("inner cone angle");
			OuterConeAngle = (int)values.GetInteger("outer cone angle");
			OuterConeGain = (int)values.GetInteger("outer cone gain");
			Flags = (int)values.GetInteger("flags");
			Azimuth = (int)values.GetInteger("azimuth");
			PositionalGain = (int)values.GetInteger("position gain");
			FirstPersonGain = (int)values.GetInteger("first person gain");
		}
	}
}

