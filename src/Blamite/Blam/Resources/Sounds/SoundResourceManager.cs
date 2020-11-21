using System.Linq;
using Blamite.Blam.Resources;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.Resources.Sounds
{
	public class SoundResourceManager
	{
		private readonly MetaAllocator _allocator;
		private readonly EngineDescription _buildInfo;
		private readonly SoundResourceGestalt _gestalt;
		private readonly FileSegmentGroup _metaArea;
		private readonly TagTable _tags;
		private readonly IPointerExpander _expander;

		/// <summary>
		///     Initializes a new instance of the <see cref="SoundResourceManager" /> class.
		/// </summary>
		/// <param name="gestalt">The cache file's sound resource gestalt.</param>
		/// <param name="layoutTable">The cache file's resource layout table.</param>
		/// <param name="tags">The cache file's tag table.</param>
		/// <param name="metaArea">The cache file's meta area.</param>
		/// <param name="allocator">The cache file's tag data allocator.</param>
		/// <param name="buildInfo">The cache file's build information.</param>
		public SoundResourceManager(SoundResourceGestalt gestalt, TagTable tags, FileSegmentGroup metaArea,
			MetaAllocator allocator, EngineDescription buildInfo, IPointerExpander expander)
		{
			_gestalt = gestalt;
			_tags = tags;
			_metaArea = metaArea;
			_allocator = allocator;
			_buildInfo = buildInfo;
			_expander = expander;
		}

		/// <summary>
		///     Loads the sound resource table from the cache file.
		/// </summary>
		/// <param name="reader">The stream to read from.</param>
		/// <returns>
		///     The loaded sound resource table, or <c>null</c> if loading failed.
		/// </returns>
		public SoundResourceTable LoadSoundResourceTable(IReader reader)
		{
			if (_gestalt == null)
				return null;

			var result = new SoundResourceTable();
			result.Codecs.AddRange(_gestalt.LoadCodecs(reader));
			result.Playbacks.AddRange(_gestalt.LoadPlaybacks(reader));
			result.Scales.AddRange(_gestalt.LoadScales(reader));
			result.CustomPlaybacks.AddRange(_gestalt.LoadCustomPlaybacks(_tags, reader));
			result.ExtraInfos.AddRange(_gestalt.LoadExtraInfos(reader));
			result.PitchRanges.AddRange(_gestalt.LoadPitchRanges(reader));
			result.LanguageDurations.AddRange(_gestalt.LoadLanguageDurations(reader));
			result.Promotions.AddRange(_gestalt.LoadPromotions(reader));
			return result;
		}

		/// <summary>
		///     Saves the sound resource table back to the file.
		/// </summary>
		/// <param name="table">The sound resource table to save.</param>
		/// <param name="stream">The stream to save to.</param>
		public void SaveSoundResourceTable(SoundResourceTable table, IStream stream)
		{
			if (_gestalt == null)
				return;

			_gestalt.SaveCodecs(table.Codecs, stream);
			_gestalt.SavePlaybacks(table.Playbacks, stream);
			_gestalt.SaveScales(table.Scales, stream);
			_gestalt.SaveCustomPlaybacks(table.CustomPlaybacks, stream);
			_gestalt.SaveExtraInfos(table.ExtraInfos, stream);
			_gestalt.SavePitchRanges(table.PitchRanges, stream);
			_gestalt.SaveLanguageDurations(table.LanguageDurations, stream);
			_gestalt.SavePromotions(table.Promotions, stream);
		}
	}
}
