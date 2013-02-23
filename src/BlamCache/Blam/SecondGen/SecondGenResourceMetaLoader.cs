using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.IO;

namespace ExtryzeDLL.Blam.SecondGen
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
    }
}
