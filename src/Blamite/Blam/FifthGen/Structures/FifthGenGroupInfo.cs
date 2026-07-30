namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     Information about a tag group found while mounting a Campaign Evolved tag namespace.
	/// </summary>
	/// <remarks>
	///     A CE tag carries only its own four-CC (the <c>group_tag</c> field of its 64-byte header -
	///     see <c>ce-format-spec.md</c> section 3.1); there is nothing in a mounted container that
	///     describes group inheritance the way third-generation tag group definitions do, so
	///     <see cref="ParentMagic" /> and <see cref="GrandparentMagic" /> are always -1 and
	///     <see cref="Description" /> is always <see cref="StringID.Null" /> - there being no
	///     StringID table for Campaign Evolved to describe it with in the first place (see
	///     <see cref="FifthGenCacheFile.StringIDs" />).
	/// </remarks>
	public class FifthGenGroupInfo : ITagGroup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenGroupInfo" /> class.
		/// </summary>
		/// <param name="magic">The group's four-CC, packed the way <see cref="Util.CharConstant" /> expects.</param>
		public FifthGenGroupInfo(int magic)
		{
			Magic = magic;
			ParentMagic = -1;
			GrandparentMagic = -1;
			Description = StringID.Null;
		}

		public int Magic { get; set; }
		public int ParentMagic { get; set; }
		public int GrandparentMagic { get; set; }
		public StringID Description { get; set; }
	}
}
