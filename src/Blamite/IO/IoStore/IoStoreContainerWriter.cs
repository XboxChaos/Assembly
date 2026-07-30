using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Blamite.Serialization;

namespace Blamite.IO.IoStore
{
	/// <summary>
	///     Rewrites an IoStore <c>.utoc</c>/<c>.ucas</c> container pair with one chunk's content replaced.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This never patches a container in place: the new chunk content is very unlikely to be the same length as
	///         the old, and even a same-length payload can land inside a compressed block whose neighbours this reader
	///         cannot re-derive without decompressing them - and Campaign Evolved mod containers hold no compressed
	///         blocks to begin with (<c>CompressionMethodNameCount</c> is <c>0</c> on every one sampled), so there is
	///         nothing to gain from special-casing "same size" beyond what a fresh write already does for free. Every
	///         chunk's current bytes are read out of the original container, one is substituted, and the whole
	///         <c>.utoc</c>/<c>.ucas</c> pair is rebuilt from that - which is what a modding tool producing a fresh
	///         override <c>_P</c> container would do anyway.
	///     </para>
	///     <para>
	///         The layout this writes was reverse-derived from the five sample mod containers, not from any published
	///         specification, and is deliberately narrow about what it will touch:
	///     </para>
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 A chunk's logical offset is always quantized up to the next multiple of the container's
	///                 compression block size, even when the chunk immediately before it left the current block only
	///                 partially full - confirmed by every chunk boundary in both a three-chunk and a much larger
	///                 sixty-nine-block sample container.
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 Physically, in the <c>.ucas</c> file, blocks are packed back to back with no padding at all -
	///                 the logical quantization has no physical counterpart. A block's declared uncompressed size is
	///                 exactly however many bytes of its chunk are left for it to cover, capped at one compression
	///                 block size, never padded out to it.
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 Every block this writes is stored (compression method 0): Campaign Evolved mod containers carry
	///                 no compression method names to declare a real one with, and this codebase has no Oodle
	///                 <em>compressor</em> in the first place, only a decompressor for reading a shipped-game container
	///                 (which none of the test data exercises either). Writing a compressed block is refused outright
	///                 rather than pretended at.
	///             </description>
	///         </item>
	///     </list>
	///     <para>
	///         What this does not attempt, refusing rather than guessing at: a compressed source container
	///         (<see cref="IoStoreTableOfContents.IsCompressed" />), a signed one
	///         (<see cref="IoStoreTableOfContents.IsSigned" />), one with a non-empty directory index
	///         (<see cref="IoStoreTableOfContents.DirectoryIndexSize" /> - every sampled mod container's is zero), or
	///         one split across more than one partition. None of those shapes turned up in any of the five sample
	///         containers, so nothing here has been checked against one.
	///     </para>
	/// </remarks>
	public static class IoStoreContainerWriter
	{
		/// <summary>
		///     Replaces one chunk's content in a container, rewriting the <c>.utoc</c> and its sibling <c>.ucas</c>.
		/// </summary>
		/// <param name="tocPath">The path to the container's <c>.utoc</c> file. The sibling <c>.ucas</c> is derived from it.</param>
		/// <param name="layouts">The layout collection holding the <see cref="IoStoreTableOfContents.TocHeaderLayoutName" /> layout.</param>
		/// <param name="chunkId">The ID of the chunk whose content is being replaced.</param>
		/// <param name="newContent">The chunk's new content.</param>
		/// <exception cref="IoStoreException">
		///     Thrown if the container does not hold <paramref name="chunkId" />, or has a shape this writer does not
		///     support - see this class's remarks.
		/// </exception>
		public static void ReplaceChunk(string tocPath, StructureLayoutCollection layouts, IoChunkId chunkId, byte[] newContent)
		{
			ReplaceChunk(tocPath, Path.ChangeExtension(tocPath, ".ucas"), layouts, chunkId, newContent);
		}

		/// <summary>
		///     Replaces one chunk's content in a container, rewriting the <c>.utoc</c> and <c>.ucas</c> at explicit paths.
		/// </summary>
		/// <param name="tocPath">The path to the container's <c>.utoc</c> file.</param>
		/// <param name="casPath">The path to the container's <c>.ucas</c> file.</param>
		/// <param name="layouts">The layout collection holding the <see cref="IoStoreTableOfContents.TocHeaderLayoutName" /> layout.</param>
		/// <param name="chunkId">The ID of the chunk whose content is being replaced.</param>
		/// <param name="newContent">The chunk's new content.</param>
		/// <exception cref="IoStoreException">
		///     Thrown if the container does not hold <paramref name="chunkId" />, or has a shape this writer does not
		///     support - see this class's remarks.
		/// </exception>
		public static void ReplaceChunk(string tocPath, string casPath, StructureLayoutCollection layouts, IoChunkId chunkId,
			byte[] newContent)
		{
			if (newContent == null)
				throw new ArgumentNullException(nameof(newContent));

			IoStoreTableOfContents toc;
			byte[][] chunkContents;

			// Every chunk's current bytes are read - and the container's streams closed again - before anything is
			// written, so that writing to the same paths never races an open read handle on them.
			using (IoStoreContainer container = IoStoreContainer.Open(tocPath, casPath, layouts, null))
			{
				toc = container.TableOfContents;
				ValidateSupported(toc, tocPath);

				int targetIndex = toc.FindChunk(chunkId);
				if (targetIndex < 0)
				{
					throw new IoStoreException(string.Format(
						"\"{0}\" does not hold chunk {1}, so its content cannot be replaced.", tocPath, chunkId));
				}

				chunkContents = new byte[toc.ChunkIds.Count][];
				for (var i = 0; i < chunkContents.Length; i++)
					chunkContents[i] = (i == targetIndex) ? newContent : container.ReadChunk(i);
			}

			WriteContainer(tocPath, casPath, layouts, toc, chunkContents);
		}

		/// <summary>
		///     Checks that a container's shape is one this writer actually knows how to reproduce, refusing outright
		///     rather than silently mishandling anything it does not.
		/// </summary>
		private static void ValidateSupported(IoStoreTableOfContents toc, string tocPath)
		{
			if (toc.IsCompressed || toc.CompressionMethods.Count > 0)
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" declares compressed blocks or compression methods. This writer only writes stored " +
					"(uncompressed) blocks, which is the only shape any Campaign Evolved mod container sampled uses.",
					tocPath));
			}
			if (toc.IsSigned)
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" carries signature hashes, which this writer does not know how to recompute.", tocPath));
			}
			if (toc.DirectoryIndexSize > 0)
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" carries a {1} byte directory index. This writer only handles the empty (size zero) " +
					"directory index every sampled mod container carries; rewriting a populated one is not implemented.",
					tocPath, toc.DirectoryIndexSize));
			}
			if (toc.PartitionCount != 1)
			{
				throw new IoStoreException(string.Format(
					"\"{0}\" is split across {1} partitions. This writer only handles a single partition.", tocPath,
					toc.PartitionCount));
			}
		}

		private static void WriteContainer(string tocPath, string casPath, StructureLayoutCollection layouts,
			IoStoreTableOfContents toc, byte[][] chunkContents)
		{
			int blockSize = toc.CompressionBlockSize;
			var locations = new IoChunkLocation[chunkContents.Length];
			var blocks = new List<IoCompressedBlock>();

			string tempCasPath = casPath + ".assembly-tmp";
			string tempTocPath = tocPath + ".assembly-tmp";

			try
			{
				using (var casStream = new FileStream(tempCasPath, FileMode.Create, FileAccess.Write))
				{
					long logicalCursor = 0;
					long physicalCursor = 0;

					for (var i = 0; i < chunkContents.Length; i++)
					{
						byte[] content = chunkContents[i];
						long start = RoundUpToBlockBoundary(logicalCursor, blockSize);
						locations[i] = new IoChunkLocation(start, content.Length);

						var offset = 0;
						while (offset < content.Length)
						{
							int take = Math.Min(blockSize, content.Length - offset);
							casStream.Write(content, offset, take);
							blocks.Add(new IoCompressedBlock(physicalCursor, take, take, 0));
							physicalCursor += take;
							offset += take;
						}

						logicalCursor = start + content.Length;
					}
				}

				WriteToc(tempTocPath, layouts, toc, locations, blocks);

				// Both new files are complete on disk before either replaces the mod's own filenames, so a failure
				// partway through the writes above never leaves a half-written container behind under a real name.
				ReplaceFile(tempCasPath, casPath);
				ReplaceFile(tempTocPath, tocPath);
			}
			finally
			{
				TryDelete(tempCasPath);
				TryDelete(tempTocPath);
			}
		}

		/// <summary>
		///     Rounds a logical offset up to the container's next block boundary, or leaves it alone if it is already on
		///     one - see this class's remarks for why every chunk starts on one, confirmed against real containers.
		/// </summary>
		private static long RoundUpToBlockBoundary(long value, int blockSize)
		{
			long remainder = value%blockSize;
			return (remainder == 0) ? value : value + (blockSize - remainder);
		}

		private static void WriteToc(string path, StructureLayoutCollection layouts, IoStoreTableOfContents toc,
			IoChunkLocation[] locations, List<IoCompressedBlock> blocks)
		{
			using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				var writer = new EndianWriter(stream, Endian.LittleEndian);

				WriteHeader(writer, layouts, toc, locations.Length, blocks.Count);

				foreach (IoChunkId id in toc.ChunkIds)
					id.Write(writer);

				foreach (IoChunkLocation location in locations)
					location.Write(writer);

				// Neither interpreted nor recomputed - see IoStoreTableOfContents.RawPerfectHashData. Chunk identity
				// (what this array is keyed by) is unchanged by a content replacement, so replaying it verbatim is
				// exact, not approximate.
				writer.WriteBlock(toc.RawPerfectHashData);

				foreach (IoCompressedBlock block in blocks)
					block.Write(writer);

				foreach (string name in toc.CompressionMethods)
					writer.WriteAscii(name, toc.CompressionMethodNameLength);

				// No signature block: ValidateSupported already refused a signed container. No directory index
				// bytes: ValidateSupported already refused a non-empty one, so DirectoryIndexSize is 0 and there is
				// nothing left to write - the file simply ends here, as every sampled mod container's does.
			}
		}

		private static void WriteHeader(EndianWriter writer, StructureLayoutCollection layouts, IoStoreTableOfContents toc,
			int entryCount, int blockCount)
		{
			var values = new StructureValueCollection();
			values.SetRaw("magic", Encoding.ASCII.GetBytes(IoStoreTableOfContents.Magic));
			values.SetInteger("version", IoStoreTableOfContents.SupportedVersion);
			values.SetInteger("toc header size", IoStoreTableOfContents.HeaderSizeInBytes);
			values.SetInteger("entry count", (ulong) entryCount);
			values.SetInteger("compressed block count", (ulong) blockCount);
			values.SetInteger("compressed block entry size", IoCompressedBlock.SizeInBytes);
			values.SetInteger("compression method name count", (ulong) toc.CompressionMethods.Count);
			values.SetInteger("compression method name length", (ulong) toc.CompressionMethodNameLength);
			values.SetInteger("compression block size", (ulong) toc.CompressionBlockSize);
			values.SetInteger("directory index size", (ulong) toc.DirectoryIndexSize);
			values.SetInteger("partition count", toc.PartitionCount);
			values.SetInteger("container id", toc.ContainerId);
			values.SetRaw("encryption key guid", toc.EncryptionKeyGuid);
			values.SetInteger("container flags", (ulong) (uint) toc.Flags);
			values.SetInteger("perfect hash seed count", (ulong) toc.PerfectHashSeedCount);
			values.SetInteger("partition size", toc.PartitionSize);
			values.SetInteger("chunks without perfect hash count", (ulong) toc.ChunksWithoutPerfectHashCount);

			// Built in a scratch buffer, sized to the whole fixed header, rather than written straight to the file:
			// StructureWriter only visits the fields the layout declares, so the reserved gaps between them - always
			// zero in every sampled container - have to come from somewhere, and a freshly zeroed array is a more
			// dependable source for that than whatever bytes happen to already be at those file offsets.
			var headerBytes = new byte[IoStoreTableOfContents.HeaderSizeInBytes];
			using (var headerStream = new MemoryStream(headerBytes))
			{
				var headerWriter = new EndianWriter(headerStream, Endian.LittleEndian);
				StructureWriter.WriteStructure(values, layouts.GetLayout(IoStoreTableOfContents.TocHeaderLayoutName), headerWriter);
			}
			writer.WriteBlock(headerBytes);
		}

		private static void ReplaceFile(string sourcePath, string destinationPath)
		{
			if (File.Exists(destinationPath))
				File.Delete(destinationPath);
			File.Move(sourcePath, destinationPath);
		}

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch (IOException)
			{
				// Best-effort cleanup of a scratch file; the container write already succeeded or already failed by
				// the time this runs, so a leftover .assembly-tmp file is a nuisance, not a correctness problem.
			}
		}
	}
}
