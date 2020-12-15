using Blamite.Blam.Resources;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blamite.Blam.FirstGen
{	
    // Just a dummy for now...
    class FirstGenResourceMetaLoader : IResourceMetaLoader
    {
        public bool SupportsRenderModels {
            get { return false; }
        }

        public IRenderModel LoadRenderModelMeta(ITag modeTag, IReader reader)
        {
            throw new NotImplementedException();
        }

        public bool SupportsSounds {
            get { return false; }
        }

        public ISound LoadSoundMeta(ITag sndTag, IReader reader)
        {
            throw new NotImplementedException();
        }

        public bool SupportsScenarioBsps {
            get { return false; }
        }

        public IScenarioBSP LoadScenarioBspMeta(ITag sbspTag, IReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
