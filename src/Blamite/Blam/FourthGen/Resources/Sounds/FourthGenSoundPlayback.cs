using Blamite.Blam.Resources.Sounds;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Resources.Sounds
{
	public class FourthGenSoundPlayback : ISoundPlayback
	{
		public FourthGenSoundPlayback(StructureValueCollection values, StringID[] stringIds)
		{
			Load(values, stringIds);
		}

		public StringID SoundName { get; private set; }

		public int ParametersIndex { get; private set; }

		public int FirstRuntimePermutationFlagIndex { get; private set; }

		public int EncodedPermutationCount { get; private set; }

		public int FirstPermutationIndex { get; private set; }

		private void Load(StructureValueCollection values, StringID[] stringIds)
		{
			SoundName = stringIds[values.GetInteger("sound name index")];
			ParametersIndex = (int)values.GetInteger("parameters index");
			FirstRuntimePermutationFlagIndex = (int)values.GetInteger("first runtime permutation flag index");
			EncodedPermutationCount = ((int)values.GetInteger("encoded permutation count") >> 4) & 63;
			FirstPermutationIndex = (int)values.GetInteger("first permutation index");
		}
	}
}
