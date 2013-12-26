using System.Collections.Generic;
using Assembly.Metro.Dialogs;
using Blamite.Blam;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.Editors
{
	/// <summary>
	/// Interaction logic for SoundEditor.xaml
	/// </summary>
	public partial class SoundEditor
	{
		private readonly TagEntry _tag;
		private readonly ICacheFile _cache;
		private readonly IStreamManager _streamManager;
		private readonly ISound _sound;
		private readonly ISoundResourceGestalt _soundResourceGestalt;
		private readonly Dictionary<int, ISoundPermutation> _soundPermutations;


		public SoundEditor(TagEntry tag, ICacheFile cache, IStreamManager streamManager)
		{
			InitializeComponent();

			_tag = tag;
			_cache = cache;
			_streamManager = streamManager;

			if (_cache.ResourceMetaLoader.SupportsSounds)
			{
				using (var reader = _streamManager.OpenRead())
				{
					//_soundResourceGestalt = _cache.(reader);
				}
			}
			else
			{
				IsEnabled = false;
				MetroMessageBox.Show("Unsupported", "Assembly doesn't support sounds on this build of Halo yet.");
			}
		}
	}
}
