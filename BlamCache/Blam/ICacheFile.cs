using System;
using System.Collections.Generic;
using ExtryzeDLL.Blam.Util;

namespace ExtryzeDLL.Blam.ThirdGen
{
    public interface ICacheFile
    {
        ICacheFileInfo Info { get; }
        PointerConverter MetaPointerConverter { get; }
        PointerConverter LocalePointerConverter { get; }
        IFileNameSource FileNames { get; }
        IStringIDSource StringIDs { get; }
        IList<ILanguage> Languages { get; }
        IList<ITagClass> TagClasses { get; }
        IList<ITag> Tags { get; }
        IScenario Scenario { get; }
    }
}
