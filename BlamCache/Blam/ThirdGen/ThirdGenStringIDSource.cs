using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.ThirdGen;
using ExtryzeDLL.Blam.ThirdGen.Structures;
using ExtryzeDLL.IO;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Util;
using System.IO;

namespace ExtryzeDLL.Blam.ThirdGen
{
    /// <summary>
    /// Loads the stringID table from a cache file and makes it available for outside use.
    /// </summary>
    public class ThirdGenStringIDSource : IStringIDSource
    {
        private ThirdGenStringTable _strings;
        private BuildInformation _buildInfo;

        public ThirdGenStringIDSource(IReader reader, int count, int tableSize, Pointer indexTableLocation, Pointer dataLocation, BuildInformation buildInfo)
        {
            _buildInfo = buildInfo;
            _strings = new ThirdGenStringTable(reader, count, tableSize, indexTableLocation, dataLocation, buildInfo.StringIDKey);
        }

        /// <summary>
        /// All of the strings in the file.
        /// </summary>
        public IList<string> RawStrings
        {
            get { return _strings.Strings; }
        }

        /// <summary>
        /// Returns the string that corresponds with the specified StringID.
        /// </summary>
        /// <param name="id">The StringID of the string to retrieve.</param>
        /// <returns>The string if it exists, or null otherwise.</returns>
        public string GetString(StringID id)
        {
            int index = StringIDToIndex(id);
            if (index > 0 && index < RawStrings.Count)
                return RawStrings[index];
            return null;
        }

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the RawStrings list.</param>
        /// <returns>The stringID associated with the index.</returns>
        public int StringIDToIndex(StringID id)
        {
            if (_buildInfo.StringIDResolver != null)
                return _buildInfo.StringIDResolver.StringIDToIndex(id);
            else
                return id.Value;
        }

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the RawStrings list.</param>
        /// <returns>The stringID associated with the index.</returns>
        public StringID IndexToStringID(int index)
        {
            if (_buildInfo.StringIDResolver != null)
                return _buildInfo.StringIDResolver.IndexToStringID(index);
            else
                return new StringID(index);
        }
    }
}
