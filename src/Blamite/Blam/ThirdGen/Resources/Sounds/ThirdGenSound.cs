using Blamite.Blam.Resources.Sounds;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources.Sounds
{
	public class ThirdGenSound : ISound
	{
		public ThirdGenSound(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, 
			EngineDescription buildInfo)
		{
			Load(values, reader, metaArea, buildInfo);
		}

		public byte SoundClass { get; private set; }

		public SampleRate SampleRate { get; private set; }

		public Encoding Encoding { get; private set; }

		public byte CodecIndex { get; private set; }

		public short PlaybackIndex { get; private set; }

		public byte PermutationChunkCount { get; private set; }

		public DatumIndex ResourceIndex { get; private set; }

		public int MaxPlaytime { get; private set; }

		private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea,
			EngineDescription buildInfo)
		{
			SoundClass = (byte)values.GetInteger("sound class");
			SampleRate = (SampleRate)values.GetInteger("sample rate");
			Encoding = (Encoding)values.GetInteger("encoding");
			CodecIndex = (byte)values.GetInteger("codec index");
			PlaybackIndex = (short)values.GetInteger("playback index");
			PermutationChunkCount = (byte)values.GetInteger("permutation chunk count");
			ResourceIndex = new DatumIndex(values.GetInteger("resource datum index"));
			MaxPlaytime = (short)values.GetInteger("max playtime");
		}
	}
}

