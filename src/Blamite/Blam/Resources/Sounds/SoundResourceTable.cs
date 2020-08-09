using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.Resources.Sounds
{
	/// <summary>
	///     A table of sound information.
	/// </summary>
	public class SoundResourceTable
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="SoundResourceTable" /> class.
		/// </summary>
		public SoundResourceTable()
		{
			Codecs = new List<SoundCodec>();
			Playbacks = new List<SoundPlayback>();
			Scales = new List<SoundScale>();
			CustomPlaybacks = new List<SoundCustomPlayback>();
			ExtraInfos = new List<SoundExtraInfo>();
			PitchRanges = new List<SoundPitchRange>();
			LanguageDurations = new List<SoundLanguageDuration>();
			Promotions = new List<SoundPromotion>();
		}

		/// <summary>
		///     Gets a list of codecs in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundCodec> Codecs { get; private set; }

		/// <summary>
		///     Gets a list of playbacks in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundPlayback> Playbacks { get; private set; }

		/// <summary>
		///     Gets a list of scales in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundScale> Scales { get; private set; }

		/// <summary>
		///     Gets a list of custom playbacks in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundCustomPlayback> CustomPlaybacks { get; private set; }

		/// <summary>
		///     Gets a list of extra infos in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundExtraInfo> ExtraInfos { get; private set; }

		/// <summary>
		///     Gets a list of pitch ranges in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundPitchRange> PitchRanges { get; private set; }

		/// <summary>
		///     Gets a list of language durations in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundLanguageDuration> LanguageDurations { get; private set; }

		/// <summary>
		///     Gets a list of promotions in the table.
		/// </summary>
		/// <seealso cref="SoundResourceGestalt" />
		public List<SoundPromotion> Promotions { get; private set; }

	}
}
