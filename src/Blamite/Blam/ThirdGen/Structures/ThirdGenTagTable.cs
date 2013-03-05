/* Copyright 2012 Aaron Dierking, TJ Tunnell, Jordan Mueller, Alex Reed
 * 
 * This file is part of ExtryzeDLL.
 * 
 * Extryze is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Extryze is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ExtryzeDLL.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.ThirdGen;
using Blamite.Blam.ThirdGen.Structures;
using Blamite.IO;
using Blamite.Flexibility;
using Blamite.Util;
using Blamite.Blam.Util;

namespace Blamite.Blam.ThirdGen.Structures
{
    public class ThirdGenTagTable : TagTable
    {
        private List<ITagClass> _classes;
        private List<ITag> _tags;

        public ThirdGenTagTable()
        {
            _classes = new List<ITagClass>();
            _tags = new List<ITag>();
        }

        public ThirdGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(reader, headerValues, metaArea, buildInfo);
        }

        public IList<ITagClass> Classes
        {
            get { return _classes.AsReadOnly(); }
        }

        public override ITag this[int index]
        {
            get { return _tags[index]; }
        }

        public override IEnumerator<ITag> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        private void Load(IReader reader, StructureValueCollection values, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            if (values.GetInteger("magic") != CharConstant.FromString("tags"))
                throw new ArgumentException("Invalid index table header magic");

            // Classes
            int numClasses = (int)values.GetInteger("number of classes");
            SegmentPointer classTableLocation = SegmentPointer.FromPointer(values.GetInteger("class table address"), metaArea);
            _classes = ReadClasses(reader, classTableLocation, numClasses, buildInfo);

            // Tags
            int numTags = (int)values.GetInteger("number of tags");
            SegmentPointer tagTableLocation = SegmentPointer.FromPointer(values.GetInteger("tag table address"), metaArea);
            _tags = ReadTags(reader, tagTableLocation, numTags, buildInfo, metaArea);
        }

        private List<ITagClass> ReadClasses(IReader reader, SegmentPointer classTableLocation, int numClasses, BuildInformation buildInfo)
        {
            StructureLayout layout = buildInfo.GetLayout("class entry");

            List<ITagClass> result = new List<ITagClass>();
            reader.SeekTo(classTableLocation.AsOffset());
            for (int i = 0; i < numClasses; i++)
            {
                StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                result.Add(new ThirdGenTagClass(values));
            }
            return result;
        }

        private List<ITag> ReadTags(IReader reader, SegmentPointer tagTableLocation, int numTags, BuildInformation buildInfo, FileSegmentGroup metaArea)
        {
            StructureLayout layout = buildInfo.GetLayout("tag entry");

            List<ITag> result = new List<ITag>();
            reader.SeekTo(tagTableLocation.AsOffset());
            for (int i = 0; i < numTags; i++)
            {
                StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                result.Add(new ThirdGenTag(values, (ushort)i, metaArea, _classes));
            }
            return result;
        }
    }
}
