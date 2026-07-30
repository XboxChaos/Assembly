namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     Where a <see cref="FifthGen.FifthGenFileNameSource" />'s answer for a tag's name came
	///     from.
	/// </summary>
	/// <remarks>
	///     A shipped Campaign Evolved container carries no tag-name table of any kind (see
	///     <c>ce-format-spec.md</c> section 3.9), and a mod's override containers carry no directory
	///     index either (<c>DirectoryIndexSize == 0</c> on every one sampled), so a usable name has
	///     to be recovered from whatever each container happens to offer. This is recorded per tag,
	///     in the order it is tried, so a caller can tell a real name from a fallback at a glance
	///     instead of just seeing a string with no indication of how much to trust it.
	/// </remarks>
	public enum FifthGenNameOrigin
	{
		/// <summary>
		///     No name could be produced at all. Never actually returned - <see cref="PackageIdHex" />
		///     always succeeds - but kept as the "nothing yet" default.
		/// </summary>
		None,

		/// <summary>
		///     The mounting container's own <c>FIoDirectoryIndexResource</c> named the chunk directly.
		///     The most trustworthy source there is, but every mod container sampled ships with a
		///     zero-size directory index, so this only ever fires for containers built with one (the
		///     shipped game's own paks, going by the spec - untested here).
		/// </summary>
		DirectoryIndex,

		/// <summary>
		///     The container's <c>ContainerHeader</c> chunk (IoChunkType 6) named the package.
		///     Investigated, not assumed - see the remarks on
		///     <see cref="FifthGen.FifthGenTagTable" />'s container-header handling. Every
		///     <c>ContainerHeader</c> chunk examined turned out to hold only numeric IDs (a signature,
		///     a version, a container ID, and a per-package array of package IDs and binary
		///     "store entry" metadata) and no string data whatsoever, so this source is not expected
		///     to ever actually produce a name; it is kept as a documented, checked dead end rather
		///     than silently skipped.
		/// </summary>
		ContainerHeader,

		/// <summary>
		///     The mounting container's own filename, with a trailing "_P" and its extension
		///     stripped, stood in for the tag's name. This is the fallback that actually fires for
		///     every mod tag sampled: a real container is cooked as
		///     <c>&lt;name&gt;-&lt;group_longname&gt;_P.utoc</c>, one tag per container, so the
		///     filename alone recovers a name identical to the leaf of the tag's true package path -
		///     just without the folder hierarchy in front of it, which nothing in a directory-index-
		///     less container can supply.
		/// </summary>
		ContainerFilename,

		/// <summary>
		///     Nothing else was available - typically because a single container held more than one
		///     Blam tag and the filename could not be attributed to any one of them - so the tag is
		///     named after its raw package ID instead.
		/// </summary>
		PackageIdHex
	}
}
