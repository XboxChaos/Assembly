using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Resources;
using System.Linq;

namespace Blamite.Injection
{
	/// <summary>
	///     A file containing information about tags and resources which were extracted from a cache file
	///     and which can be injected into a new one.
	/// </summary>
	public class TagContainer
	{
		private readonly Dictionary<uint, DataBlock> _dataBlocksByAddress = 
			new Dictionary<uint, DataBlock>();

		private readonly Dictionary<int, ResourcePage> _pagesByIndex = 
			new Dictionary<int, ResourcePage>();

		private readonly Dictionary<DatumIndex, ExtractedResourceInfo> _resourcesByIndex =
			new Dictionary<DatumIndex, ExtractedResourceInfo>();

		private readonly Dictionary<int, ExtractedPage> _extractedResourcePageByPageIndex =
			new Dictionary<int, ExtractedPage>();

		private readonly Dictionary<DatumIndex, ExtractedTag> _tagsByIndex = 
			new Dictionary<DatumIndex, ExtractedTag>();

		private readonly List<ExtractedResourcePredictionD> _extractedPredictions =
			new List<ExtractedResourcePredictionD>();

		private readonly Dictionary<int, ExtractedSoundCodec> _soundCodecsByIndex =
			new Dictionary<int, ExtractedSoundCodec>();

		private readonly Dictionary<int, ExtractedSoundPitchRange> _soundPitchRangesByIndex =
			new Dictionary<int, ExtractedSoundPitchRange>();

		private readonly Dictionary<int, ExtractedSoundLanguageDuration> _soundLanguageDurationsByIndex =
			new Dictionary<int, ExtractedSoundLanguageDuration>();

		private readonly Dictionary<int, ExtractedSoundPlayback> _soundPlaybacksByIndex =
			new Dictionary<int, ExtractedSoundPlayback>();

		private readonly Dictionary<int, ExtractedSoundScale> _soundScalesByIndex =
			new Dictionary<int, ExtractedSoundScale>();

		private readonly Dictionary<int, ExtractedSoundPromotion> _soundPromotionsByIndex =
			new Dictionary<int, ExtractedSoundPromotion>();

		private readonly Dictionary<int, ExtractedSoundCustomPlayback> _soundCustomPlaybacksByIndex =
			new Dictionary<int, ExtractedSoundCustomPlayback>();

		private readonly Dictionary<int, ExtractedSoundExtraInfo> _soundExtraInfoByIndex =
			new Dictionary<int, ExtractedSoundExtraInfo>();

		/// <summary>
		///     Gets a collection of all data blocks in the container.
		/// </summary>
		public ICollection<DataBlock> DataBlocks
		{
			get { return _dataBlocksByAddress.Values; }
		}

		/// <summary>
		///     Gets a collection of all tags in the container.
		/// </summary>
		public ICollection<ExtractedTag> Tags
		{
			get { return _tagsByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all resource page information objects in the container.
		/// </summary>
		public ICollection<ResourcePage> ResourcePages
		{
			get { return _pagesByIndex.Values; }
		}

		/// <summary>
		///     
		/// </summary>
		public ICollection<ExtractedPage> ExtractedResourcePages
		{
			get { return _extractedResourcePageByPageIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all resource information objects in the container.
		/// </summary>
		public ICollection<ExtractedResourceInfo> Resources
		{
			get { return _resourcesByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all resource prediction objects in the container.
		/// </summary>
		public ICollection<ExtractedResourcePredictionD> Predictions
		{
			get { return _extractedPredictions; }
		}

		/// <summary>
		///     Gets a collection of all sound codec objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundCodec> SoundCodecs
		{
			get { return _soundCodecsByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound pitch range objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundPitchRange> SoundPitchRanges
		{
			get { return _soundPitchRangesByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound language duration objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundLanguageDuration> SoundLanguageDurations
		{
			get { return _soundLanguageDurationsByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound playback objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundPlayback> SoundPlaybacks
		{
			get { return _soundPlaybacksByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound scale objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundScale> SoundScales
		{
			get { return _soundScalesByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound promotion objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundPromotion> SoundPromotions
		{
			get { return _soundPromotionsByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound custom playback objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundCustomPlayback> SoundCustomPlaybacks
		{
			get { return _soundCustomPlaybacksByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all sound extra info objects in the container.
		/// </summary>
		public ICollection<ExtractedSoundExtraInfo> SoundExtraInfos
		{
			get { return _soundExtraInfoByIndex.Values; }
		}

		/// <summary>
		///     Adds a data block to the container.
		/// </summary>
		/// <param name="block">The block to add.</param>
		public void AddDataBlock(DataBlock block)
		{
			_dataBlocksByAddress[block.OriginalAddress] = block;
		}

		/// <summary>
		///     Adds information about an extracted tag to the container.
		/// </summary>
		/// <param name="tag">The tag information to add.</param>
		public void AddTag(ExtractedTag tag)
		{
			_tagsByIndex[tag.OriginalIndex] = tag;
		}

		/// <summary>
		///     Adds information about a resource page to the container.
		/// </summary>
		/// <param name="page">The page to add.</param>
		public void AddResourcePage(ResourcePage page)
		{
			_pagesByIndex[page.Index] = page;
		}

		/// <summary>
		///     
		/// </summary>
		/// <param name="extractedPage"></param>
		/// <param name="page"></param>
		public void AddExtractedResourcePage(ExtractedPage extractedPage)
		{
			_extractedResourcePageByPageIndex[extractedPage.ResourcePageIndex] = extractedPage;
		}

		/// <summary>
		///     Adds information about a resource to the container.
		/// </summary>
		/// <param name="resource">The resource to add.</param>
		public void AddResource(ExtractedResourceInfo resource)
		{
			_resourcesByIndex[resource.OriginalIndex] = resource;
		}

		/// <summary>
		///     Adds information about a resource prediction to the container.
		/// </summary>
		/// <param name="prediction">The prediction to add.</param>
		public void AddPrediction(ExtractedResourcePredictionD prediction)
		{
			_extractedPredictions.Add(prediction);
		}

		/// <summary>
		///     Adds information about a sound codec to the container.
		/// </summary>
		/// <param name="codec">The codec to add.</param>
		public void AddSoundCodec(ExtractedSoundCodec codec)
		{
			_soundCodecsByIndex[codec.OriginalIndex] = codec;
		}

		/// <summary>
		///		Adds information about a sound pitch range to the container.
		/// </summary>
		/// <param name="pitchRange">The pitch range to add.</param>
		public void AddSoundPitchRange(ExtractedSoundPitchRange pitchRange)
		{
			_soundPitchRangesByIndex[pitchRange.OriginalIndex] = pitchRange;
		}

		/// <summary>
		///     Adds information about a sound language duration to the container.
		/// </summary>
		/// <param name="langDuration">The language duration to add.</param>
		public void AddSoundLanguageDuration(ExtractedSoundLanguageDuration langDuration)
		{
			_soundLanguageDurationsByIndex[langDuration.OriginalIndex] = langDuration;
		}

		/// <summary>
		///     Adds information about a sound playback to the container.
		/// </summary>
		/// <param name="playback">The playback to add.</param>
		public void AddSoundPlayback(ExtractedSoundPlayback playback)
		{
			_soundPlaybacksByIndex[playback.OriginalIndex] = playback;
		}

		/// <summary>
		///     Adds information about a sound scale to the container.
		/// </summary>
		/// <param name="scale">The scale to add.</param>
		public void AddSoundScale(ExtractedSoundScale scale)
		{
			_soundScalesByIndex[scale.OriginalIndex] = scale;
		}

		/// <summary>
		///     Adds information about a sound promotion to the container.
		/// </summary>
		/// <param name="promotion">The promotion to add.</param>
		public void AddSoundPromotion(ExtractedSoundPromotion promotion)
		{
			_soundPromotionsByIndex[promotion.OriginalIndex] = promotion;
		}

		/// <summary>
		///     Adds information about a sound custom playback to the container.
		/// </summary>
		/// <param name="customPlayback">The custom playback to add.</param>
		public void AddSoundCustomPlayback(ExtractedSoundCustomPlayback customPlayback)
		{
			_soundCustomPlaybacksByIndex[customPlayback.OriginalIndex] = customPlayback;
		}

		/// <summary>
		///     Adds information about a sound extra info to the container.
		/// </summary>
		/// <param name="extraInfo">The extra info to add.</param>
		public void AddSoundExtraInfo(ExtractedSoundExtraInfo extraInfo)
		{
			_soundExtraInfoByIndex[extraInfo.OriginalIndex] = extraInfo;
		}

		/// <summary>
		///     Finds the data block which has a specified original address.
		/// </summary>
		/// <param name="originalAddress">The original address of the data block to find.</param>
		/// <returns>The <see cref="DataBlock" /> with the original address, or <c>null</c> if not found.</returns>
		public DataBlock FindDataBlock(uint originalAddress)
		{
			DataBlock result;
			_dataBlocksByAddress.TryGetValue(originalAddress, out result);
			return result;
		}

		/// <summary>
		///     Finds the tag which has a specified original datum index.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the tag to find.</param>
		/// <returns>The <see cref="ExtractedTag" /> with the original datum index, or <c>null</c> if not found.</returns>
		public ExtractedTag FindTag(DatumIndex originalIndex)
		{
			ExtractedTag result;
			_tagsByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="originalIndex"></param>
		/// <returns></returns>
		public ExtractedPage FindExtractedResourcePage(int originalIndex)
		{
			ExtractedPage result;
			_extractedResourcePageByPageIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the resource page which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the resource page to find.</param>
		/// <returns>The <see cref="ResourcePage" /> with the original index, or <c>null</c> if not found.</returns>
		public ResourcePage FindResourcePage(int originalIndex)
		{
			ResourcePage result;
			_pagesByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the resource which has a specified original datum index.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the resource to find.</param>
		/// <returns>The <see cref="ExtractedResourceInfo" /> with the original datum index, or <c>null</c> if not found.</returns>
		public ExtractedResourceInfo FindResource(DatumIndex originalIndex)
		{
			ExtractedResourceInfo result;
			_resourcesByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound codec which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound codec to find.</param>
		/// <returns>The <see cref="ExtractedSoundCodec" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundCodec FindSoundCodec(int originalIndex)
		{
			ExtractedSoundCodec result;
			_soundCodecsByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound pitch range which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound pitch range to find.</param>
		/// <returns>The <see cref="ExtractedSoundPitchRange" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundPitchRange FindSoundPitchRange(int originalIndex)
		{
			ExtractedSoundPitchRange result;
			_soundPitchRangesByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound language duration which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound language duration to find.</param>
		/// <returns>The <see cref="ExtractedSoundLanguageDuration" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundLanguageDuration FindSoundLanguageDuration(int originalIndex)
		{
			ExtractedSoundLanguageDuration result;
			_soundLanguageDurationsByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound playback which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound playback to find.</param>
		/// <returns>The <see cref="ExtractedSoundPlayback" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundPlayback FindSoundPlayback(int originalIndex)
		{
			ExtractedSoundPlayback result;
			_soundPlaybacksByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound scale which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound scale to find.</param>
		/// <returns>The <see cref="ExtractedSoundScale" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundScale FindSoundScale(int originalIndex)
		{
			ExtractedSoundScale result;
			_soundScalesByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound promotion which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound promotion to find.</param>
		/// <returns>The <see cref="ExtractedSoundPromotion" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundPromotion FindSoundPromotion(int originalIndex)
		{
			ExtractedSoundPromotion result;
			_soundPromotionsByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound custom playback which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound custom playback to find.</param>
		/// <returns>The <see cref="ExtractedSoundCustomPlayback" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundCustomPlayback FindSoundCustomPlayback(int originalIndex)
		{
			ExtractedSoundCustomPlayback result;
			_soundCustomPlaybacksByIndex.TryGetValue(originalIndex, out result);
			return result;
		}

		/// <summary>
		///     Finds the sound extra info which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the sound extra info to find.</param>
		/// <returns>The <see cref="ExtractedSoundExtraInfo" /> with the original index, or <c>null</c> if not found.</returns>
		public ExtractedSoundExtraInfo FindSoundExtraInfo(int originalIndex)
		{
			ExtractedSoundExtraInfo result;
			_soundExtraInfoByIndex.TryGetValue(originalIndex, out result);
			return result;
		}
	}
}