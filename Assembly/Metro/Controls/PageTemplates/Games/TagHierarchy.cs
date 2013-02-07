using System.Collections.Generic;
using System.Collections.ObjectModel;
using ExtryzeDLL.Blam;

namespace Assembly.Metro.Controls.PageTemplates.Games
{
    public class TagHierarchy
    {
        public ObservableCollection<TagClass> Classes { get; set; }
        public List<TagEntry> Entries { get; set; }
    }

    public class TagClass
    {
        public TagClass(ITagClass baseClass, string name, string description)
        {
            RawClass = baseClass;
            TagClassMagic = name;
            Description = description;
            Children = new List<TagEntry>();
        }

        public string TagClassMagic { get; set; }
        public string Description { get; set; }
        public ITagClass RawClass { get; set; }
        public List<TagEntry> Children { get; private set; }
    }

    public class TagEntry
    {
        public TagEntry(ITag baseTag, TagClass parent, string name)
        {
            RawTag = baseTag;
            TagFileName = name;
            ParentClass = parent;
        }

        public string TagFileName { get; private set; }
        public ITag RawTag { get; private set; }
        public TagClass ParentClass { get; private set; }
    }
}
