using Blamite.Blam.Resources.Sounds;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Sounds
{
	public class FourthGenSoundPermutationChunk : ISoundPermutationChunk
	{
		public FourthGenSoundPermutationChunk(StructureValueCollection values)
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

