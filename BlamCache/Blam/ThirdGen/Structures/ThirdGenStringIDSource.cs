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

namespace ExtryzeDLL.Blam.ThirdGen.Structures
{
    public class ThirdGenStringIDSource : IStringIDSource
    {
        private ThirdGenStringTable _strings;
        private BuildInformation _buildInfo;

        public ThirdGenStringIDSource(IReader reader, int count, int tableSize, Pointer indexTableLocation, Pointer dataLocation, BuildInformation buildInfo)
        {
            _buildInfo = buildInfo;
            _strings = new ThirdGenStringTable(reader, count, tableSize, indexTableLocation, dataLocation, buildInfo.StringIDKey);
        }

        public string GetString(int id)
        {
            int index = StringIDToIndex(id);
            if (index > 0 && index < RawStrings.Count)
                return RawStrings[index];
            return null;
        }

        public IList<string> RawStrings
        {
            get { return _strings.Strings; }
        }

        public int StringIDToIndex(int id)
        {
            if (_buildInfo.StringIDModifiers != null)
            {
                foreach (BuildInformation.StringIDModifier modifier in _buildInfo.StringIDModifiers)
                {
                    bool modify = false;

                    if (modifier.isGreaterThan)
                        modify = (id > modifier.Identifier);
                    else
                        modify = (id < modifier.Identifier);

                    if (modify)
                    {
                        if (modifier.isAddition)
                            id += modifier.Modifier;
                        else
                            id -= modifier.Modifier;
                    }
                }
            }
            return id;
        }

        public int IndexToStringID(int index)
        {
            if (_buildInfo.StringIDModifiers != null)
            {
                foreach (BuildInformation.StringIDModifier modifier in _buildInfo.StringIDModifiers)
                {
                    bool modify = false;

                    if (modifier.isGreaterThan)
                        modify = (index > modifier.Identifier);
                    else
                        modify = (index < modifier.Identifier);

                    if (modify)
                    {
                        if (modifier.isAddition)
                            index -= modifier.Modifier;
                        else
                            index += modifier.Modifier;
                    }
                }
            }
            return index;
        }
    }
}
