using System;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;

namespace Blamite.Blam.SecondGen
{
	// Just a dummy for now...
	public class SecondGenResourceMetaLoader : IResourceMetaLoader
	{
		public bool SupportsRenderModels
		{
			get { return false; }
		}

		public IRenderModel LoadRenderModelMeta(ITag modeTag, IReader reader)
		{
			throw new NotImplementedException();
		}

		public bool SupportsSounds
		{
			get { return false; }
		}

		public ISound LoadSoundMeta(ITag sndTag, IReader reader)
		{
			throw new NotImplementedException();
		}

		public bool SupportsScenarioBsps
		{
			get { return false; }
		}

		public IScenarioBSP LoadScenarioBspMeta(ITag sbspTag, IReader reader)
		{
			throw new NotImplementedException();
		}
	}
}