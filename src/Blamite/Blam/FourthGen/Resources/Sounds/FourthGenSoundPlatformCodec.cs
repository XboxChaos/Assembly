using Blamite.Blam.Resources.Sounds;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Sounds
{
	class FourthGenSoundPlatformCodec : ISoundPlatformCodec
	{
		public FourthGenSoundPlatformCodec(StructureValueCollection values)
		{
			Load(values);
		}

		public Channel Channel { get; private set; }

		public void Load(StructureValueCollection values)
		{
			Channel = (Channel)values.GetInteger("channel");
		}
	}
}
