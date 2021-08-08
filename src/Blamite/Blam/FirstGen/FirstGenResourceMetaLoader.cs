using System;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;

namespace Blamite.Blam.FirstGen
{	
	// Just a dummy for now...
	public class FirstGenResourceMetaLoader : IResourceMetaLoader
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

		public CacheSound LoadSoundMeta(ITag sndTag, IReader reader)
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
