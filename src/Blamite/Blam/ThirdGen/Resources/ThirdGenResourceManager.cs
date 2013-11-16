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
    /// <summary>
    /// Third-gen implementation of <see cref="IResourceManager"/>.
    /// </summary>
    public class ThirdGenResourceManager : IResourceManager
    {
        private ThirdGenResourceGestalt _gestalt;
        private ThirdGenResourceLayoutTable _layoutTable;
        private TagTable _tags;
        private FileSegmentGroup _metaArea;
        private MetaAllocator _allocator;
        private EngineDescription _buildInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThirdGenResourceManager"/> class.
        /// </summary>
        /// <param name="gestalt">The cache file's resource gestalt.</param>
        /// <param name="layoutTable">The cache file's resource layout table.</param>
        /// <param name="tags">The cache file's tag table.</param>
        /// <param name="metaArea">The cache file's meta area.</param>
        /// <param name="allocator">The cache file's tag data allocator.</param>
        /// <param name="buildInfo">The cache file's build information.</param>
        public ThirdGenResourceManager(ThirdGenResourceGestalt gestalt, ThirdGenResourceLayoutTable layoutTable, TagTable tags, FileSegmentGroup metaArea, MetaAllocator allocator, EngineDescription buildInfo)
        {
            _gestalt = gestalt;
            _layoutTable = layoutTable;
            _tags = tags;
            _metaArea = metaArea;
            _allocator = allocator;
            _buildInfo = buildInfo;
        }

        /// <summary>
        /// Loads the resource table from the cache file.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <returns>
        /// The loaded resource table, or <c>null</c> if loading failed.
        /// </returns>
        public ResourceTable LoadResourceTable(IReader reader)
        {
            ResourceTable result = new ResourceTable();
            result.Pages.AddRange(_layoutTable.LoadPages(reader));
            var pointers = _layoutTable.LoadPointers(reader, result.Pages);
            result.Resources.AddRange(_gestalt.LoadResources(reader, _tags, pointers.ToList()));
            return result;
        }

        /// <summary>
        /// Saves the resource table back to the file.
        /// </summary>
        /// <param name="table">The resource table to save.</param>
        /// <param name="stream">The stream to save to.</param>
        public void SaveResourceTable(ResourceTable table, IStream stream)
        {
            var pointers = _gestalt.SaveResources(table.Resources, stream);
            _layoutTable.SavePointers(pointers, stream);
            _layoutTable.SavePages(table.Pages, stream);
        }

        /// <summary>
        /// Loads the zone set table from the cache file.
        /// </summary>
        /// <param name="reader">The stream to read from.</param>
        /// <returns>
        /// The loaded zone set table, or <c>null</c> if loading failed.
        /// </returns>
        public IZoneSetTable LoadZoneSets(IReader reader)
        {
            return new ThirdGenZoneSetTable(_gestalt, reader, _metaArea, _allocator, _buildInfo);
        }
    }
}
