using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Resources.Sounds
{
	/// <summary>
	/// The possible sample rates that a Sound can have.
	/// </summary>
	public enum SoundSampleRate
	{
		_22050,
		_44100,
		_32000,
		UnknownMCC
	}

	/// <summary>
	/// Types of Compression that a Sound can have.
	/// </summary>
	public enum SoundEncoding
	{
		Mono,
		Stereo,
		Surround,
		_51_Surround,
		Codec
	}

	/// <summary>
	/// Types of Compression that a Sound can have.
	/// </summary>
	public enum SoundCompression
	{
		NoneBigEndian,
		XboxADPCM,
		IMAADPCM,
		NoneLittleEndian,
		WMA,
		NoneAgnostic,
		XMA,
		XMA2,
		UnknownMCC
	}

	public class CacheSound
	{
		public CacheSound(StructureValueCollection values)
		{
			Load(values);
		}

		public int Flags { get; private set; }

		/// <summary>
		/// Gets the Sound Class of the Sound.
		/// </summary>
		public int SoundClass { get; private set; }

		public int PitchRangeCount { get; private set; }

		public int CodecIndex { get; private set; }

		public int FirstPitchRangeIndex { get; private set; }

		public int FirstLanguageDurationPitchRangeIndex { get; private set; }

		public int SubPriority { get; private set; }

		public int PlaybackIndex { get; private set; }

		public int ScaleIndex { get; private set; }

		public int PromotionIndex { get; private set; }

		public int CustomPlaybackIndex { get; private set; }

		public int ExtraInfoIndex { get; private set; }

		/// <summary>
		/// Gets the Max Playtime of the Sound.
		/// </summary>
		public int MaxPlaytime { get; set; }

		public DatumIndex ResourceIndex { get; set; }

		private void Load(StructureValueCollection values)
		{
			CodecIndex = (short)values.GetInteger("codec index");
			PitchRangeCount = (short)values.GetInteger("pitch range count");
			FirstPitchRangeIndex = (short)values.GetInteger("first pitch range index");
			FirstLanguageDurationPitchRangeIndex = (short)values.GetInteger("first language duration pitch range index");
			SubPriority = (short)values.GetInteger("sub priority");
			PlaybackIndex = (short)values.GetInteger("playback index");
			ScaleIndex = (short)values.GetInteger("scale index");
			PromotionIndex = (sbyte)values.GetInteger("promotion index");
			CustomPlaybackIndex = (sbyte)values.GetInteger("custom playback index");
			ExtraInfoIndex = (short)values.GetInteger("extra info index");
			ResourceIndex = new DatumIndex(values.GetInteger("resource datum index"));
			MaxPlaytime = (short)values.GetInteger("max playtime");
		}
	}
}