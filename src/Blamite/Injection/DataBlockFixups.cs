using Blamite.Blam;

namespace Blamite.Injection
{
	/// <summary>
	///     Contains information about an address in a <see cref="DataBlock" /> which needs to be changed to point to
	///     newly-injected data.
	/// </summary>
	public class DataBlockAddressFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockAddressFixup" /> class.
		/// </summary>
		/// <param name="originalAddress">The original address of the data.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new address of the injected data to.</param>
		public DataBlockAddressFixup(uint originalAddress, int writeOffset)
		{
			OriginalAddress = originalAddress;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original address of the data that was pointed to.
		///     This can be used to find the data inside the tag container.
		/// </summary>
		public uint OriginalAddress { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new address of the injected data to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a tag reference in a <see cref="DataBlock" /> which needs to be changed to point to a
	///     tag in the target cache file.
	/// </summary>
	public class DataBlockTagFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockTagFixup" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the tag.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new datum index of the tag to.</param>
		public DataBlockTagFixup(DatumIndex originalIndex, int writeOffset)
		{
			OriginalIndex = originalIndex;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original datum index of the tag that was pointed to.
		///     This can be used to find the tag inside the tag container.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new datum index of the tag to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a resource reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a newly-injected resource.
	/// </summary>
	public class DataBlockResourceFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockResourceFixup" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the resouce.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new datum index of the resource to.</param>
		public DataBlockResourceFixup(DatumIndex originalIndex, int writeOffset)
		{
			OriginalIndex = originalIndex;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original datum index of the resource that was pointed to.
		///     This can be used to find the resource inside the tag container.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new datum index of the resource to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a stringID reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a stringID in the new cache file.
	/// </summary>
	public class DataBlockStringIDFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockStringIDFixup" /> class.
		/// </summary>
		/// <param name="originalString">The original string for the stringID.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new stringID to.</param>
		public DataBlockStringIDFixup(string originalString, int writeOffset)
		{
			OriginalString = originalString;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original string for the stringID.
		/// </summary>
		public string OriginalString { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new stringID to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a shader reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a shader in the new cache file.
	/// </summary>
	public class DataBlockShaderFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockShaderFixup"/> class.
		/// </summary>
		/// <param name="writeOffset">The write offset.</param>
		/// <param name="data">The data.</param>
		public DataBlockShaderFixup(int writeOffset, byte[] data)
		{
			WriteOffset = writeOffset;
			Data = data;
		}

		/// <summary>
		/// Gets the offset to write the shader pointer to.
		/// </summary>
		public int WriteOffset { get; private set; }

		/// <summary>
		/// Gets the shader data to import.
		/// </summary>
		public byte[] Data { get; private set; }
	}

	public class UnicListFixupString
	{
		public UnicListFixupString(string stringId, string str)
		{
			StringID = stringId;
			String = str;
		}

		public string StringID { get; private set; }
		public string String { get; private set; }
	}

	/// <summary>
	/// Contains information about a multilingual unicode string list in a <see cref="DataBlock"/> which needs to be injected into the cache file.
	/// </summary>
	public class DataBlockUnicListFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockUnicListFixup"/> class.
		/// </summary>
		/// <param name="writeOffset">The write offset.</param>
		/// <param name="strings">The strings.</param>
		public DataBlockUnicListFixup(int languageIndex, int writeOffset, UnicListFixupString[] strings)
		{
			LanguageIndex = languageIndex;
			WriteOffset = writeOffset;
			Strings = strings;
		}

		/// <summary>
		/// Gets the index of the language that the fixup contains strings for.
		/// </summary>
		public int LanguageIndex { get; private set; }

		/// <summary>
		/// Gets the offset to write the updated list info to.
		/// </summary>
		public int WriteOffset { get; private set; }

		/// <summary>
		/// Gets the strings to inject into the cache file.
		/// </summary>
		public UnicListFixupString[] Strings { get; private set; }
	}

	/// <summary>
	/// Contains information about a interop pointer in a <see cref="DataBlock"/> which needs to be injected into the cache file.
	/// </summary>
	public class DataBlockInteropFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockInteropFixup"/> class.
		/// </summary>
		/// <param name="type">The type index of interop from the cache's interop table.</param>
		/// <param name="originalAddress">The original address to this interop.</param>
		/// <param name="writeOffset">The write offset.</param>
		public DataBlockInteropFixup(int type, uint originalAddress, int writeOffset)
		{
			Type = type;
			OriginalAddress = originalAddress;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the type of interop.
		/// </summary>
		public int Type { get; private set; }

		/// <summary>
		///     Gets the original address of the data that was pointed to.
		///     This can be used to find the data inside the tag container.
		/// </summary>
		public uint OriginalAddress { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new address of the injected data to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	/// Contains information about a compiled effect entry interop in a <see cref="DataBlock"/> which needs to be injected into the cache file.
	/// </summary>
	public class DataBlockEffectFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockEffectFixup"/> class.
		/// </summary>
		/// <param name="type">The type index of interop from the cache's interop table.</param>
		/// <param name="originalAddress">The original address to this interop.</param>
		/// <param name="writeOffset">The write offset.</param>
		/// <param name="data">The associated compiled data.</param>
		public DataBlockEffectFixup(int type, int originalIndex, int writeOffset, byte[] data)
		{
			Type = type;
			OriginalIndex = originalIndex;
			WriteOffset = writeOffset;
			Data = data;
		}

		/// <summary>
		///     Gets the type of interop.
		/// </summary>
		public int Type { get; private set; }

		/// <summary>
		///     Gets the original index of the data.
		///     This can be used to find the data inside the tag container.
		/// </summary>
		public int OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new address of the injected data to.
		/// </summary>
		public int WriteOffset { get; private set; }

		/// <summary>
		/// Gets the effect data to import.
		/// </summary>
		public byte[] Data { get; private set; }
	}

	/// <summary>
	/// Contains information about a sound in a <see cref="DataBlock"/> which needs to be injected into the cache file.
	/// </summary>
	public class DataBlockSoundFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockSoundFixup"/> class.
		/// </summary>
		/// <param name="originalCodecIndex">The original codec index of a sound.</param>
		/// <param name="pitchRangeCount">The pitch range count of a sound.</param>
		/// <param name="originalPitchRangeIndex">The original pitch range index of a sound.</param>
		/// <param name="originalLanguageDurationIndex">The original language duration index of a sound.</param>
		/// <param name="originalPlaybackIndex">The original playback index of a sound.</param>
		/// <param name="originalScaleIndex">The original scale index of a sound.</param>
		/// <param name="originalPromotionIndex">The original promotion index of a sound.</param>
		/// <param name="originalCustomPlaybackIndex">The original custom playback index of a sound.</param>
		/// <param name="originalExtraInfoIndex">The original extra info index of a sound.</param>
		public DataBlockSoundFixup(int originalCodecIndex, int pitchRangeCount, int originalPitchRangeIndex, int originalLanguageDurationIndex,
			int originalPlaybackIndex, int originalScaleIndex, int originalPromotionIndex, int originalCustomPlaybackIndex, int originalExtraInfoIndex)
		{
			OriginalCodecIndex = originalCodecIndex;
			PitchRangeCount = pitchRangeCount;
			OriginalPitchRangeIndex = originalPitchRangeIndex;
			OriginalLanguageDurationIndex = originalLanguageDurationIndex;
			OriginalPlaybackIndex = originalPlaybackIndex;
			OriginalScaleIndex = originalScaleIndex;
			OriginalPromotionIndex = originalPromotionIndex;
			OriginalCustomPlaybackIndex = originalCustomPlaybackIndex;
			OriginalExtraInfoIndex = originalExtraInfoIndex;
		}

		public int OriginalCodecIndex { get; private set; }
		public int PitchRangeCount { get; private set; }
		public int OriginalPitchRangeIndex { get; private set; }
		public int OriginalLanguageDurationIndex { get; private set; }
		public int OriginalPlaybackIndex { get; private set; }
		public int OriginalScaleIndex { get; private set; }
		public int OriginalPromotionIndex { get; private set; }
		public int OriginalCustomPlaybackIndex { get; private set; }
		public int OriginalExtraInfoIndex { get; private set; }
	}
}
