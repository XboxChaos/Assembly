using Blamite.Blam.Resources.Sounds;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	class ThirdGenSoundPlatformCodec : ISoundPlatformCodec
	{
		public ThirdGenSoundPlatformCodec(StructureValueCollection values)
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
