using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Resources;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.Blam.Util;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Resources
{
    public class ThirdGenResourceGestalt : IResourceTable
    {
        private ThirdGenResource[] _resources;

        public ThirdGenResourceGestalt(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, ThirdGenTagTable tags, ThirdGenResourceLayoutTable layoutInfo)
        {
            Load(values, reader, metaArea, buildInfo, tags, layoutInfo);
        }

        /// <summary>
        /// Looks up a resource by its datum index and returns it.
        /// </summary>
        /// <param name="index">The datum index of the resource to look up.</param>
        /// <returns>The resource with the corresponding index.</returns>
        public IResource this[DatumIndex index]
        {
            get { return _resources[index.Index]; }
        }

        public IEnumerator<IResource> GetEnumerator()
        {
            return ((IEnumerable<IResource>)_resources).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        private void Load(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, ThirdGenTagTable tags, ThirdGenResourceLayoutTable layoutInfo)
        {
            LoadResources(values, reader, metaArea, buildInfo, tags, layoutInfo);
        }

        private void LoadResources(StructureValueCollection values, IReader reader, FileSegmentGroup metaArea, BuildInformation buildInfo, ThirdGenTagTable tags, ThirdGenResourceLayoutTable layoutInfo)
        {
            int count = (int)values.GetInteger("number of resources");
            uint address = values.GetInteger("resource table address");
            var layout = buildInfo.GetLayout("resource table entry");
            var entries = ReflexiveReader.ReadReflexive(count, address, reader, layout, metaArea);

            _resources = new ThirdGenResource[entries.Length];
            for (ushort i = 0; i < entries.Length; i++)
                _resources[i] = new ThirdGenResource(entries[i], i, tags, layoutInfo);
        }
    }
}
