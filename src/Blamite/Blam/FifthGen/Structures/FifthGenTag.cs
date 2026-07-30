using Blamite.IO;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     A single Blam tag mounted from a Campaign Evolved IoStore container: the decompressed
	///     bytes of one <c>BulkData</c> (<c>.ubulk</c>) chunk that begins with a 64-byte
	///     <c>BLAM</c>/<c>MALB</c> tag header.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Unlike every other engine's <see cref="ITag" />, a CE tag's data is not addressable
	///         through <see cref="MetaLocation" /> - there is no shared file or address space for a
	///         <see cref="SegmentPointer" /> to point into, since each tag's bytes were read from a
	///         different container's chunk table. <see cref="MetaLocation" /> is always <c>null</c>
	///         here; <see cref="RawPayload" /> is how a tag's bytes are actually reached, and it is
	///         the narrow seam this engine hands off to a tag body parser - the full 64-byte header
	///         included; only the group four-CC and the endianness marker in it are read here.
	///     </para>
	/// </remarks>
	public class FifthGenTag : ITag
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTag" /> class.
		/// </summary>
		/// <param name="index">The tag's slot in its <see cref="FifthGen.FifthGenTagTable" />.</param>
		/// <param name="group">The tag's group, identified by the four-CC in its header.</param>
		/// <param name="packageId">The UE package ID shared by this tag's <c>.uasset</c>/<c>.ubulk</c> chunk pair.</param>
		/// <param name="rawPayload">The tag's raw <c>.ubulk</c> bytes, 64-byte header included.</param>
		/// <param name="container">The mounted container this tag's data currently comes from.</param>
		public FifthGenTag(DatumIndex index, ITagGroup group, FifthGenPackageId packageId, byte[] rawPayload,
			FifthGenMountedContainer container)
		{
			Index = index;
			Group = group;
			PackageId = packageId;
			RawPayload = rawPayload;
			Container = container;
			MetaLocation = null;
			Source = TagSource.FifthGen;
		}

		public DatumIndex Index { get; private set; }
		public ITagGroup Group { get; set; }
		public SegmentPointer MetaLocation { get; set; }
		public TagSource Source { get; set; }

		/// <summary>
		///     Gets the UE package ID shared by this tag's <c>.uasset</c> and <c>.ubulk</c> chunks -
		///     the value their <see cref="Blamite.IO.IoStore.IoChunkId.PackageId" /> carries.
		/// </summary>
		public FifthGenPackageId PackageId { get; private set; }

		/// <summary>
		///     Gets the tag's raw <c>.ubulk</c> bytes verbatim: a self-describing, Halo Reach-era MCC
		///     tag file, 64-byte header included. This engine only reads that header (see
		///     <see cref="FifthGen.FifthGenTagTable" />); everything past it - the <c>blay</c>/<c>bdat</c>
		///     schema and data - is left for a tag body parser to consume.
		/// </summary>
		/// <remarks>
		///     Updated by <see cref="FifthGen.FifthGenCacheFile.SaveChanges" /> once a pending edit has actually been
		///     written back to the owning container, so that a caller reading this straight back afterward - without
		///     re-mounting anything - sees the saved bytes rather than the ones this tag was constructed with.
		/// </remarks>
		public byte[] RawPayload { get; internal set; }

		/// <summary>
		///     Gets the mounted container this tag's data was last read from - the one that currently
		///     wins for <see cref="PackageId" /> once every mounted container's overrides are applied
		///     (see <see cref="FifthGen.FifthGenTagTable" />).
		/// </summary>
		public FifthGenMountedContainer Container { get; private set; }

		/// <summary>
		///     Gets or sets a parsed, possibly-edited view of this tag awaiting a save.
		/// </summary>
		/// <remarks>
		///     <para>
		///         A fifth-generation tag has no fixed-offset fields to write through the way a classic engine's does -
		///         see <see cref="FifthGenTagWriter" /> - so there is nothing for a per-field write path to hook into.
		///         Instead, a caller that has parsed <see cref="RawPayload" /> into a <see cref="FifthGenTagFile" /> and
		///         edited it through the mutation methods on <see cref="FifthGenTagValue" />,
		///         <see cref="FifthGenTagStruct" /> and <see cref="FifthGenTagBlock" /> hands that same instance back
		///         here, and the next <see cref="FifthGen.FifthGenCacheFile.SaveChanges" /> call serialises it and writes
		///         it into this tag's owning container.
		///     </para>
		///     <para>
		///         <c>null</c> - the default - means there is nothing pending. Set back to <c>null</c> once
		///         <see cref="FifthGen.FifthGenCacheFile.SaveChanges" /> has written it, both so a repeat call does not
		///         redundantly rewrite the container and so this property's own value answers "is a save pending"
		///         honestly.
		///     </para>
		/// </remarks>
		public FifthGenTagFile PendingEdit { get; set; }
	}
}
