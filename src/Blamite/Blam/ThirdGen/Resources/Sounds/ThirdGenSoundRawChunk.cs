using Blamite.Blam.Resources.Sounds;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	public class ThirdGenSoundRawChunk : ISoundRawChunk
	{
		public ThirdGenSoundRawChunk(StructureValueCollection values)
		{
			Load(values);
		}

		public int Offset { get; private set; }

		public int Size { get; private set; }

		public int RuntimeIndex { get; private set; }

		private void Load(StructureValueCollection values)
		{
			Offset = (int)values.GetInteger("offset");
			Size = (int)values.GetInteger("size");
			RuntimeIndex = (int)values.GetInteger("runtime index");
		}
	}
}

