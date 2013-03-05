using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;
using ExtryzeDLL.IO;
using ExtryzeDLL.Util;

namespace ExtryzeDLL.Blam.SecondGen.Structures
{
    public class SecondGenTagTable : TagTable
    {
        private List<ITagClass> _classes;
        private Dictionary<int, ITagClass> _classesById;
        private List<ITag> _tags;

        public SecondGenTagTable(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            Load(reader, headerValues, metaArea, buildInfo);
        }

        public IList<ITagClass> Classes
        {
            get { return _classes; }
        }

        public override ITag this[int index]
        {
            get { return _tags[index]; }
        }

        public override IEnumerator<ITag> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        private void Load(IReader reader, StructureValueCollection headerValues, FileSegmentGroup metaArea, BuildInformation buildInfo)
        {
            if (headerValues.GetInteger("magic") != CharConstant.FromString("tags"))
                throw new ArgumentException("Invalid index table header magic");

            // Classes
            int numClasses = (int)headerValues.GetInteger("number of classes");
            uint classTableOffset = (uint)(metaArea.Offset + headerValues.GetInteger("class table offset")); // Offset is relative to the header
            _classes = ReadClasses(reader, classTableOffset, numClasses, buildInfo);
            _classesById = BuildClassLookup(_classes);

            // Tags
            int numTags = (int)headerValues.GetInteger("number of tags");
            uint tagTableOffset = (uint)(metaArea.Offset + headerValues.GetInteger("tag table offset")); // Offset is relative to the header
            _tags = ReadTags(reader, tagTableOffset, numTags, buildInfo, metaArea);
        }

        private static List<ITagClass> ReadClasses(IReader reader, uint classTableOffset, int numClasses, BuildInformation buildInfo)
        {
            StructureLayout layout = buildInfo.GetLayout("class entry");

            List<ITagClass> result = new List<ITagClass>();
            reader.SeekTo(classTableOffset);
            for (int i = 0; i < numClasses; i++)
            {
                StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                result.Add(new SecondGenTagClass(values));
            }
            return result;
        }

        private static Dictionary<int, ITagClass> BuildClassLookup(List<ITagClass> classes)
        {
            Dictionary<int, ITagClass> result = new Dictionary<int, ITagClass>();
            foreach (ITagClass tagClass in classes)
                result[tagClass.Magic] = tagClass;
            return result;
        }

        private List<ITag> ReadTags(IReader reader, uint tagTableOffset, int numTags, BuildInformation buildInfo, FileSegmentGroup metaArea)
        {
            StructureLayout layout = buildInfo.GetLayout("tag entry");

            List<ITag> result = new List<ITag>();
            reader.SeekTo(tagTableOffset);
            for (int i = 0; i < numTags; i++)
            {
                StructureValueCollection values = StructureReader.ReadStructure(reader, layout);
                result.Add(new SecondGenTag(values, metaArea, _classesById));
            }
            return result;
        }
    }
}
