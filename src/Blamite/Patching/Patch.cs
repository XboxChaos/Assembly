using System.Collections.Generic;

namespace Blamite.Patching
{
	public class Patch
	{
		public Patch()
		{
			MetaChanges = new List<DataChange>();
			LanguageChanges = new List<LanguageChange>();
			SegmentChanges = new List<SegmentChange>();
			MapID = -1;
			MetaChangesIndex = -1;
		}

		/// <summary>
		///     The ID of the .map file that the patch is meant for. -1 if this information is not present.
		/// </summary>
		public int MapID { get; set; }

		/// <summary>
		///     The internal name of the .map file that the patch is meant for. Can be null.
		/// </summary>
		public string MapInternalName { get; set; }

		/// <summary>
		///     The name of the patch.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		///     A short description of the patch.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		///     The patch's author.
		/// </summary>
		public string Author { get; set; }

		/// <summary>
		///     A screenshot of the patch in action.
		/// </summary>
		public byte[] Screenshot { get; set; }

		/// <summary>
		///     Changes which should be made to different parts of a map file.
		/// </summary>
		public List<SegmentChange> SegmentChanges { get; private set; }

		/// <summary>
		///     The base pointer to add to meta change offsets to get a pointer to poke to.
		/// </summary>
		public uint MetaPokeBase { get; set; }

		/// <summary>
		///     The index in <see cref="SegmentChanges" /> of the file's meta area changes.
		///     Can be -1 if no meta change information is present in <see cref="SegmentChanges" />.
		/// </summary>
		public int MetaChangesIndex { get; set; }

		/// <summary>
		///     Embedded BLF and mapinfo files. Defaults to null.
		/// </summary>
		public BlfContent CustomBlfContent { get; set; }

		#region Deprecated

		/// <summary>
		///     [DEPRECATED] Changes which should be made to the map file's meta area.
		///     The offset of each change is the meta pointer to where the change should be made.
		/// </summary>
		public List<DataChange> MetaChanges { get; private set; }

		/// <summary>
		///     [DEPRECATED] Changes that should be made to a map's locales.
		/// </summary>
		public List<LanguageChange> LanguageChanges { get; private set; }

		#endregion Deprecated
	}
}