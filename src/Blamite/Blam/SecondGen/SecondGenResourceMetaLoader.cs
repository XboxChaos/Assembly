using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources;
using Blamite.Blam.Resources.Models;
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

        public bool SupportsScenarioBSPs
        {
            get { return false; }
        }

        public Resources.BSP.IScenarioBSP LoadScenarioBSPMeta(ITag sbspTag, IReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
