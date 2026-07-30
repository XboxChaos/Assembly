using System.Collections.Generic;
using System.IO;
using Blamite.Blam.FifthGen.Structures;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen
{
	/// <summary>
	///     A fifth-generation Blam tag payload.
	/// </summary>
	/// <remarks>
	///     <para>
	///         A tag in this generation is not a region of a cache file. It is a whole, self-contained file cooked into an
	///         Unreal bulk data chunk, and it carries both its own schema and its own strings, so parsing one needs nothing from
	///         outside it - no cache header, no stringID table, no tag name table and no plugin. What it does need is to be
	///         handed the decompressed bytes; extracting those from the container is a separate concern.
	///     </para>
	///     <para>
	///         Because the schema comes out of the file rather than out of Layout XML, <see cref="Layout" /> is a first-class
	///         object graph rather than a <see cref="Blamite.Serialization.StructureLayout" />. See
	///         <see cref="FifthGenTagLayout" /> for why that is the right way round here.
	///     </para>
	/// </remarks>
	public class FifthGenTagFile
	{
		private readonly List<string> _warnings = new List<string>();
		private readonly Dictionary<string, byte[]> _undecodedChunks = new Dictionary<string, byte[]>();

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagFile" /> class from a decompressed payload.
		/// </summary>
		/// <param name="payload">The decompressed payload bytes.</param>
		public FifthGenTagFile(byte[] payload)
			: this(payload, 0)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagFile" /> class from a decompressed payload.
		/// </summary>
		/// <param name="payload">The decompressed payload bytes.</param>
		/// <param name="rootStructIndex">The index of the struct the root block's elements are laid out as.</param>
		public FifthGenTagFile(byte[] payload, int rootStructIndex)
		{
			using (var reader = new EndianReader(new MemoryStream(payload, false), Endian.LittleEndian))
				Load(reader, rootStructIndex);
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenTagFile" /> class from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from, positioned at the start of the payload.</param>
		/// <param name="rootStructIndex">The index of the struct the root block's elements are laid out as.</param>
		public FifthGenTagFile(IReader reader, int rootStructIndex)
		{
			Load(reader, rootStructIndex);
		}

		/// <summary>
		///     Gets the payload's sixty-four byte header.
		/// </summary>
		public FifthGenTagHeader Header { get; private set; }

		/// <summary>
		///     Gets the tag's group, as identified by the four-CC in the header.
		/// </summary>
		public FifthGenTagGroup Group { get; private set; }

		/// <summary>
		///     Gets the endianness the payload is stored in.
		/// </summary>
		public Endian Endianness
		{
			get { return Header.Endianness; }
		}

		/// <summary>
		///     Gets the schema the payload declares for itself.
		/// </summary>
		public FifthGenTagLayout Layout { get; private set; }

		/// <summary>
		///     Gets the payload's root block.
		/// </summary>
		public FifthGenTagBlock Data { get; private set; }

		/// <summary>
		///     Gets the index of the struct the root block's elements were read as.
		/// </summary>
		/// <remarks>
		///     Nothing in the payload states which struct the root block uses; the root block is emitted by the data chunk itself
		///     rather than by a field, so there is no block definition to follow. The first struct is the default because it is
		///     where a definition tree's root naturally lands, and it is safe to default because a wrong choice cannot pass
		///     unnoticed: the root block's packed size would not match and the strict consumption check turns that into an error
		///     rather than into plausible-looking rubbish. A caller which knows better can say so.
		/// </remarks>
		public int RootStructIndex { get; private set; }

		/// <summary>
		///     Gets the problems found while parsing which were not serious enough to abandon the parse: undecoded tables,
		///     unrecognized field types, inconsistent tag references and optional chunks nobody has decoded.
		/// </summary>
		public IList<string> Warnings
		{
			get { return _warnings; }
		}

		/// <summary>
		///     Gets the optional top-level chunks which were preserved verbatim rather than interpreted, keyed by magic.
		/// </summary>
		/// <remarks>
		///     A payload can carry a dependency list, an import-info chunk holding compressed source files, and an asset depot
		///     chunk. None of them is needed to read the tag's data, so they are kept as bytes.
		/// </remarks>
		public IDictionary<string, byte[]> UndecodedChunks
		{
			get { return _undecodedChunks; }
		}

		/// <summary>
		///     Gets the exact bytes this instance was parsed from.
		/// </summary>
		/// <remarks>
		///     <see cref="FifthGenTagWriter" /> is built around this: an unmodified region of a tag is proven byte-exact by
		///     never having been re-derived from the decoded model in the first place, only ever copied out of here. Keeping
		///     the whole payload rather than just, say, the <c>bdat</c> chunk's content also sidesteps needing to separately
		///     remember every chunk version this parser does not otherwise attribute anywhere in the object graph (the tag
		///     body's own version, the schema chunk's, the data chunk's) - the writer rereads them from here instead of
		///     guessing.
		/// </remarks>
		public byte[] OriginalPayload { get; private set; }

		/// <summary>
		///     Determines whether a buffer is a Blam tag payload rather than some other kind of Unreal bulk data.
		/// </summary>
		/// <param name="payload">The decompressed buffer to test.</param>
		/// <returns><c>true</c> if the buffer carries a tag signature.</returns>
		public static bool IsTagPayload(byte[] payload)
		{
			return FifthGenTagHeader.IsTagPayload(payload);
		}

		/// <summary>
		///     Determines whether a stream holds a Blam tag payload at its current position.
		/// </summary>
		/// <param name="reader">The stream to test. Its position is restored before returning.</param>
		/// <returns><c>true</c> if the stream carries a tag signature.</returns>
		public static bool IsTagPayload(IReader reader)
		{
			return FifthGenTagHeader.IsTagPayload(reader);
		}

		private void Load(IReader reader, int rootStructIndex)
		{
			RootStructIndex = rootStructIndex;
			long baseOffset = reader.Position;

			// Captured before anything else is read, and from baseOffset rather than 0, so that this still works if a
			// caller ever hands in a reader positioned partway through a larger stream rather than one scoped to exactly
			// one payload (the only shape actually exercised today - see the byte[]-based constructor).
			reader.SeekTo(baseOffset);
			OriginalPayload = reader.ReadBlock((int) (reader.Length - baseOffset));
			reader.SeekTo(baseOffset);

			Header = FifthGenTagHeader.Read(reader);
			Group = new FifthGenTagGroup(Header);

			reader.SeekTo(baseOffset + FifthGenTagHeader.Size);
			FifthGenChunk body = FifthGenChunk.ReadExpecting(reader, reader.Length, CharConstant.FromString("tag!"),
				"the tag body");

			LoadBody(reader, body, rootStructIndex);

			if (body.ContentEnd < reader.Length)
			{
				_warnings.Add(
					$"{reader.Length - body.ContentEnd} byte(s) follow the tag body at 0x{body.ContentEnd:X} and were ignored.");
			}
		}

		/// <summary>
		///     Walks the tag body's children, of which only the schema and the data are needed to read the tag.
		/// </summary>
		private void LoadBody(IReader reader, FifthGenChunk body, int rootStructIndex)
		{
			FifthGenChunk layoutChunk = null;
			FifthGenChunk dataChunk = null;

			reader.SeekTo(body.ContentOffset);
			while (reader.Position < body.ContentEnd)
			{
				FifthGenChunk chunk = FifthGenChunk.Read(reader, body.ContentEnd);
				if (chunk.Magic == CharConstant.FromString("blay"))
					layoutChunk = chunk;
				else if (chunk.Magic == CharConstant.FromString("bdat"))
					dataChunk = chunk;
				else
					PreserveChunk(reader, chunk);

				reader.SeekTo(chunk.ContentEnd);
			}
			body.EnsureConsumed(reader);

			if (layoutChunk == null)
				throw new FifthGenFormatException("The tag body has no 'blay' chunk, so its schema is unknown.");
			if (dataChunk == null)
				throw new FifthGenFormatException("The tag body has no 'bdat' chunk, so it carries no data.");

			Layout = LoadLayout(reader, layoutChunk);
			Data = LoadData(reader, dataChunk, rootStructIndex);
		}

		private FifthGenTagLayout LoadLayout(IReader reader, FifthGenChunk chunk)
		{
			return new FifthGenTagLayout(reader, chunk, _warnings);
		}

		private FifthGenTagBlock LoadData(IReader reader, FifthGenChunk chunk, int rootStructIndex)
		{
			if (Layout.Structs.Count == 0)
				throw new FifthGenFormatException("The tag layout declares no structs, so its data cannot be walked.");

			var dataReader = new FifthGenTagDataReader(Layout, _warnings);
			return dataReader.ReadData(reader, chunk, rootStructIndex);
		}

		private void PreserveChunk(IReader reader, FifthGenChunk chunk)
		{
			_undecodedChunks[chunk.MagicString] = chunk.ReadContent(reader);
			_warnings.Add(
				$"'{chunk.MagicString}' chunk at 0x{chunk.HeaderOffset:X} holds {chunk.Size} byte(s) which this parser does not interpret. They were preserved verbatim.");
		}
	}
}
