using Blamite.Serialization;
using System.Collections.Generic;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundExtraInfo
	{
		public SoundExtraInfoPermutationSection[] PermutationSections { get; set; }

		/// <summary>
		/// Storage for datums. Reach Beta can potentially have more than one, Reach only 1
		/// </summary>
		public DatumIndex[] Datums { get; set; }

		public SoundExtraInfo()
		{
		}
	}

	public class SoundExtraInfoPermutationSection
	{
		public byte[] EncodedData { get; set; }

		public SoundExtraInfoDialogueInfo[] DialogueInfos { get; set; }

		public SoundExtraInfoUnknown1[] Unknown1s { get; set; }
	}

	public class SoundExtraInfoDialogueInfo
	{
		public int MouthOffset { get; set; }

		public int MouthLength { get; set; }

		public int LipsyncOffset { get; set; }

		public int LipsyncLength { get; set; }
	}

	public class SoundExtraInfoUnknown1
	{
		public int Unknown { get; set; }

		public int Unknown1 { get; set; }

		public int Unknown2 { get; set; }

		public int Unknown3 { get; set; }

		public int Unknown4 { get; set; }

		public int Unknown5 { get; set; }

		public int Unknown6 { get; set; }

		public int Unknown7 { get; set; }

		public int Unknown8 { get; set; }

		public int Unknown9 { get; set; }

		public int Unknown10 { get; set; }

		public int Unknown11 { get; set; }

		public SoundExtraInfoUnknown2[] Unknown12s { get; set; }
	}

	public class SoundExtraInfoUnknown2
	{
		public float Unknown { get; set; }

		public float Unknown1 { get; set; }

		public float Unknown2 { get; set; }

		public float Unknown3 { get; set; }

		public SoundExtraInfoUnknown3[] Unknown5s { get; set; }

		public SoundExtraInfoUnknown4[] Unknown6s { get; set; }
	}

	public class SoundExtraInfoUnknown3
	{
		public int Unknown { get; set; }

		public int Unknown1 { get; set; }

		public int Unknown2 { get; set; }

		public int Unknown3 { get; set; }
	}

	public class SoundExtraInfoUnknown4
	{
		public int Unknown { get; set; }

		public int Unknown1 { get; set; }
	}
}