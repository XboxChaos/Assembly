using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Resources.Models;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Blam.ThirdGen.Resources.Models
{
    public class ThirdGenModelSubmesh : IModelSubmesh
    {
        public ThirdGenModelSubmesh(StructureValueCollection values)
        {
            Load(values);
        }

        public int ShaderIndex { get; private set; }

        public int IndexBufferStart { get; private set; }

        public int IndexBufferCount { get; private set; }

        public int VertexGroupStart { get; private set; }

        public int VertexGroupCount { get; private set; }

        public int VertexBufferCount { get; private set; }

        private void Load(StructureValueCollection values)
        {
            ShaderIndex = (int)values.GetNumber("shader index");
            IndexBufferStart = (int)values.GetNumber("index buffer start");
            IndexBufferCount = (int)values.GetNumber("index buffer count");
            VertexGroupStart = (int)values.GetNumber("vertex group start");
            VertexGroupCount = (int)values.GetNumber("vertex group count");
            VertexBufferCount = (int)values.GetNumber("vertex buffer count");
        }
    }
}
