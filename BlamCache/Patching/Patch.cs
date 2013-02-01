using System.Collections.Generic;

namespace ExtryzeDLL.Patching
{
    public class Patch
    {
        public Patch()
        {
            MetaChanges = new List<MetaChange>();
            LanguageChanges = new List<LanguageChange>();
            MapID = -1;
        }

        /// <summary>
        /// The ID of the .map file that the patch is meant for. -1 if this information is not present.
        /// </summary>
        public int MapID { get; set; }

        /// <summary>
        /// The internal name of the .map file that the patch is meant for. Can be null.
        /// </summary>
        public string MapInternalName { get; set; }

        /// <summary>
        /// The name of the patch.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A short description of the patch.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The patch's author.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// A screenshot of the patch in action.
        /// </summary>
        public byte[] Screenshot { get; set; }

        /// <summary>
        /// Changes that should be made to a map's meta.
        /// </summary>
        public IList<MetaChange> MetaChanges { get; private set; }

        /// <summary>
        /// Changes that should be made to a map's locales.
        /// </summary>
        public IList<LanguageChange> LanguageChanges { get; private set; }
    }
}
