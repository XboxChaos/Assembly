using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Resources.Models
{
    public class ThirdGenModelVertexGroup : IModelVertexGroup
    {
        public ThirdGenModelVertexGroup(StructureValueCollection values, IModelSubmesh[] submeshes)
        {
            Load(values, submeshes);
        }

        public int IndexBufferStart { get; private set; }

        public int IndexBufferCount { get; private set; }

        public int VertexBufferCount { get; private set; }

        public IModelSubmesh Submesh { get; private set; }

        private void Load(StructureValueCollection values, IModelSubmesh[] submeshes)
        {
            IndexBufferStart = (int)values.GetNumber("index buffer start");
            IndexBufferCount = (int)values.GetNumber("index buffer count");
            VertexBufferCount = (int)values.GetNumber("vertex buffer count");

            int submeshIndex = (int)values.GetNumber("parent submesh index");
            Submesh = submeshes[submeshIndex];
        }
    }
}
