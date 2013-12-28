using Blamite.Blam.Resources.Sounds;
using Blamite.Flexibility;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	public class ThirdGenSoundPermutation : ISoundPermutation
	{
		public ThirdGenSoundPermutation(StructureValueCollection values, StringID[] stringIds)
		{
			Load(values, stringIds);
		}

		public StringID SoundName { get; private set; }

		public int EncodedSkipFraction { get; private set; }

		public int EncodedGain { get; private set; }

		public int PermutationInfoIndex { get; private set; }

		public int LanguageNeutralTime { get; private set; }

		public int PermutationChunkIndex { get; private set; }

		public int ChunkCount { get; private set; }

		public int EncodedPermutationIndex { get; private set; }

		private void Load(StructureValueCollection values, StringID[] stringIds)
		{
			SoundName = stringIds[values.GetInteger("sound name index")];
			EncodedSkipFraction = (int)values.GetInteger("encoded skip fraction");
			EncodedGain = (int)values.GetInteger("encoded gain");
			PermutationInfoIndex = (int)values.GetInteger("permutation info index");
			LanguageNeutralTime = (int)values.GetInteger("language neutral time");
			PermutationChunkIndex = (int)values.GetInteger("raw chunk index");
			ChunkCount = (int)values.GetInteger("chunk count");
			EncodedPermutationIndex = (int)values.GetInteger("encoded permutation index");
		}
	}
}

