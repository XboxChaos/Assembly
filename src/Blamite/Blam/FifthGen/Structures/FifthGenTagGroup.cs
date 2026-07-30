using System.Collections.Generic;
using System.Text;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     The group a fifth-generation tag belongs to.
	/// </summary>
	/// <remarks>
	///     A payload states its group as a four-CC in its header and carries no group table, so there is nothing in the file
	///     to populate <see cref="ParentMagic" />, <see cref="GrandparentMagic" /> or <see cref="Description" /> with. Those
	///     are reported absent rather than invented. The group's long name - the <c>biped</c> in
	///     <c>spartans-biped.uasset</c> - comes from the cooked filename, which is why <see cref="LongName" /> is settable and
	///     may be <c>null</c> when a payload is parsed on its own.
	/// </remarks>
	public class FifthGenTagGroup : ITagGroup
	{
		/// <summary>
		///     The class every tag data asset derives from, and the safe fallback when a group's long name cannot be resolved into
		///     a class of its own.
		/// </summary>
		public const string BaseClassName = "BlamTagDataAssetBase";

		/// <summary>
		///     The prefix every generated tag data asset class name carries.
		/// </summary>
		public const string ClassPrefix = "Blam";

		/// <summary>
		///     The suffix every generated tag data asset class name carries.
		/// </summary>
		public const string ClassSuffix = "TagDataAsset";

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagGroup" /> class from a tag payload header.
		/// </summary>
		/// <param name="header">The header whose group four-CC identifies the group.</param>
		public FifthGenTagGroup(FifthGenTagHeader header)
			: this(header.GroupMagic)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagGroup" /> class.
		/// </summary>
		/// <param name="magic">The group's four-CC magic number.</param>
		public FifthGenTagGroup(int magic)
		{
			Magic = magic;
			ParentMagic = -1;
			GrandparentMagic = -1;
			Description = StringID.Null;
		}

		/// <summary>
		///     Gets or sets the group's four-CC magic number. Read from offset 0x30 of the payload header, and the authoritative
		///     statement of the tag's group.
		/// </summary>
		public int Magic { get; set; }

		/// <summary>
		///     Gets or sets the parent group's magic. Always -1: a payload carries no group hierarchy.
		/// </summary>
		public int ParentMagic { get; set; }

		/// <summary>
		///     Gets or sets the grandparent group's magic. Always -1: a payload carries no group hierarchy.
		/// </summary>
		public int GrandparentMagic { get; set; }

		/// <summary>
		///     Gets or sets the stringID describing the group's purpose. Always <see cref="StringID.Null" />: there is no stringID
		///     table in a payload to resolve one against.
		/// </summary>
		public StringID Description { get; set; }

		/// <summary>
		///     Gets or sets the group's long name, e.g. <c>frame_event_list</c>. This is <c>null</c> unless a cooked filename has
		///     been supplied, because a payload does not contain it.
		/// </summary>
		public string LongName { get; set; }

		/// <summary>
		///     Gets the group's four-CC as a printable string.
		/// </summary>
		public string GroupTag
		{
			get { return FifthGenChunk.MagicToString(Magic); }
		}

		/// <summary>
		///     Gets the name of the Unreal class a tag of this group is cooked into, derived from <see cref="LongName" />.
		/// </summary>
		public string ClassName
		{
			get { return GetClassName(LongName); }
		}

		/// <summary>
		///     Splits a cooked tag filename into its tag name and its group's long name.
		/// </summary>
		/// <param name="fileName">A filename or package name such as <c>spartans-biped</c>, with or without an extension.</param>
		/// <param name="tagName">On return, the tag's own name.</param>
		/// <param name="groupLongName">On return, the group's long name.</param>
		/// <returns><c>true</c> if the name carried a group suffix.</returns>
		/// <remarks>
		///     The split is on the <em>last</em> hyphen, because tag names themselves contain hyphens.
		/// </remarks>
		public static bool TryParseFileName(string fileName, out string tagName, out string groupLongName)
		{
			tagName = null;
			groupLongName = null;
			if (string.IsNullOrEmpty(fileName))
				return false;

			string leaf = GetLeafName(fileName);
			int extension = leaf.LastIndexOf('.');
			if (extension > 0)
				leaf = leaf.Substring(0, extension);

			int split = leaf.LastIndexOf('-');
			if (split <= 0 || split == leaf.Length - 1)
				return false;

			tagName = leaf.Substring(0, split);
			groupLongName = leaf.Substring(split + 1);
			return true;
		}

		/// <summary>
		///     Derives the name of the Unreal class a group's tags are cooked into.
		/// </summary>
		/// <param name="groupLongName">The group's long name, e.g. <c>frame_event_list</c>.</param>
		/// <returns>
		///     The class name, e.g. <c>BlamFrameEventListTagDataAsset</c>, or <see cref="BaseClassName" /> when the long name
		///     cannot be turned into one.
		/// </returns>
		/// <remarks>
		///     The rule is a prefix, the long name in Pascal case with underscores as the word separator, and a suffix. Falling
		///     back to the base class rather than failing matters: at least one shipped group's generated class is missing from
		///     both the Unreal property map and the header dump, and the base class is what such a tag has to be decoded as.
		/// </remarks>
		public static string GetClassName(string groupLongName)
		{
			if (string.IsNullOrEmpty(groupLongName))
				return BaseClassName;

			var builder = new StringBuilder(ClassPrefix);
			var words = 0;
			foreach (string word in groupLongName.Split('_'))
			{
				if (word.Length == 0)
					continue;
				builder.Append(char.ToUpperInvariant(word[0]));
				if (word.Length > 1)
					builder.Append(word.Substring(1));
				words++;
			}

			if (words == 0)
				return BaseClassName;

			builder.Append(ClassSuffix);
			return builder.ToString();
		}

		/// <summary>
		///     Cross-checks a group four-CC read from a payload header against a group long name taken from a cooked filename.
		/// </summary>
		/// <param name="magic">The four-CC from the payload header.</param>
		/// <param name="groupLongName">The long name from the filename.</param>
		/// <param name="longNamesByMagic">
		///     A four-CC to long name mapping, which the caller has to supply. A payload does not contain one.
		/// </param>
		/// <param name="expectedLongName">On return, the long name the mapping gives for the four-CC, or <c>null</c>.</param>
		/// <returns>
		///     <c>true</c> if the mapping agrees with the filename, <c>false</c> if it disagrees or does not cover the four-CC.
		/// </returns>
		/// <remarks>
		///     The four-CC is authoritative and the filename is a convenience, but nothing in a payload relates the two: the
		///     four-CC to long name mapping lives in the engine's tag definitions, not in the tag. A caller which has such a
		///     mapping - from Blamite's own group name data, for instance - can use this to confirm a filename before trusting
		///     the class name derived from it.
		/// </remarks>
		public static bool TryCrossCheckGroup(int magic, string groupLongName, IDictionary<int, string> longNamesByMagic,
			out string expectedLongName)
		{
			expectedLongName = null;
			if (longNamesByMagic == null || !longNamesByMagic.TryGetValue(magic, out expectedLongName))
				return false;

			return string.Equals(expectedLongName, groupLongName);
		}

		private static string GetLeafName(string path)
		{
			int separator = path.LastIndexOfAny(new[] {'/', '\\'});
			return (separator >= 0) ? path.Substring(separator + 1) : path;
		}
	}
}
