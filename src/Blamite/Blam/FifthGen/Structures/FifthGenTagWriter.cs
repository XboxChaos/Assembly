using System;
using System.IO;
using System.Text;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     Serialises a parsed <see cref="FifthGenTagFile" /> back into payload bytes.
	/// </summary>
	/// <remarks>
	///     <para>
	///         A tag that was not edited at all - <c>tagFile.Data.IsDirty == false</c> - is handed back as an exact copy of
	///         <see cref="FifthGenTagFile.OriginalPayload" />. No chunk is re-derived, no size is recomputed, nothing is
	///         re-encoded; the bar the byte-exact round-trip test sets is met by construction rather than by getting every
	///         detail of a from-scratch rebuild to agree with the original.
	///     </para>
	///     <para>
	///         When something <em>was</em> edited, this still does not rebuild the whole payload from the decoded model.
	///         It walks the original bytes and the decoded tree together, one chunk at a time, and at every chunk asks
	///         whether anything inside it (see <see cref="FifthGenTagValue.IsDirtyRecursive" />,
	///         <see cref="FifthGenTagStruct.IsDirty" />, <see cref="FifthGenTagBlock.IsDirty" />) was written through since
	///         it was parsed. A clean chunk - schema-side <c>blay</c> content always is, since nothing in this codebase
	///         mutates the schema; on the data side, most of a large tag usually is too, for a small edit - is copied
	///         verbatim, header and all, without being interpreted at all. Only a chunk with something dirty inside it is
	///         re-encoded, and even then only as much of it as actually needs to be: an unedited sibling field a few levels
	///         under a dirty one is still copied straight from the original bytes.
	///     </para>
	///     <para>
	///         This is not just an optimization. Two things about this format cannot be recovered from the decoded model
	///         alone, and copying real bytes for anything unedited sidesteps needing to:
	///     </para>
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 A stringID's or tag reference's text section is sometimes written with a trailing NUL and sometimes
	///                 without - <see cref="FifthGenTagDataReader" />'s own remarks on <c>DecodeString</c> say so, and
	///                 checking real sections against the decoded (and by then NUL-trimmed) text confirms it: of 171 tag
	///                 references sampled across the five real test tags, 165 have no terminator and 6 do, with no
	///                 signal anywhere else in the file to say which a given field will be. Re-encoding one that was not
	///                 touched would silently pick a spelling that might not be the one already on disk.
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 A struct-typed field's wrapper is sometimes written empty even though its fields would ordinarily
	///                 contribute sections of their own - see <see cref="FifthGenTagDataReader" />'s remarks on struct
	///                 wrapper elision. Nothing in the decoded model says an instance was built this way rather than
	///                 having genuinely empty contents; reading the wrapper's real declared size off the original bytes,
	///                 which this writer does whenever it has original bytes to read, answers it directly instead of
	///                 guessing from which fields happen to still be at their unread default.
	///             </description>
	///         </item>
	///     </list>
	///     <para>
	///         Chunk and section sizes are always recomputed, never copied, for anything this writer actually re-encodes -
	///         see <see cref="EndChunk" /> and <see cref="EndTgstChunk" />. A <c>tgst</c> section's header is unusual: the
	///         dword this format calls its version is not a version at all for that one chunk type. Across every <c>tgst</c>
	///         header in all five sample tags (6,828 of them) that dword is numerically identical to the size dword that
	///         follows it, with zero exceptions, so <see cref="EndTgstChunk" /> writes the same recomputed size into both
	///         rather than tracking a separate "version" for it anywhere.
	///     </para>
	///     <para>
	///         What this writer does not attempt: adding, removing or reordering a fifth-generation tag's top-level chunks,
	///         and pageable resource sections, which turned up in none of the five sample tags, so the section magics a
	///         freshly-written one would use (see <see cref="FifthGenFieldTypes.IsPageableResourceSection" />) are an
	///         inference from the surrounding format's conventions rather than something checked against real data.
	///     </para>
	/// </remarks>
	public static class FifthGenTagWriter
	{
		/// <summary>
		///     Serialises a tag back into payload bytes.
		/// </summary>
		/// <param name="tagFile">
		///     The tag to serialise. Its <see cref="FifthGenTagFile.Data" /> tree may be unmodified, or may carry edits made
		///     through the mutation methods on <see cref="FifthGenTagValue" />, <see cref="FifthGenTagStruct" /> and
		///     <see cref="FifthGenTagBlock" />.
		/// </param>
		/// <returns>The serialised payload.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="tagFile" /> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">
		///     <paramref name="tagFile" /> carries no <see cref="FifthGenTagFile.OriginalPayload" /> to fall back to for the
		///     spans nothing edited. Every <see cref="FifthGenTagFile" /> parsed by either of its public constructors has
		///     one; this can only happen for an instance built some other way.
		/// </exception>
		public static byte[] Write(FifthGenTagFile tagFile)
		{
			if (tagFile == null)
				throw new ArgumentNullException(nameof(tagFile));
			if (tagFile.OriginalPayload == null)
			{
				throw new InvalidOperationException(
					"This tag has no captured original payload for the writer to fall back to for the spans nothing " +
					"edited. Every FifthGenTagFile built from a byte[] or IReader has one.");
			}

			// The overwhelmingly common case - nothing was edited - is also the cheapest and the most trustworthy: the
			// bytes are simply the bytes this instance was parsed from.
			if (!tagFile.Data.IsDirty)
				return (byte[]) tagFile.OriginalPayload.Clone();

			return new Session(tagFile).Run();
		}

		/// <summary>
		///     Holds the state one <see cref="Write" /> call needs - the tag's endianness and a reader over its original
		///     bytes - so the recursive write methods do not have to thread them through every call individually.
		/// </summary>
		private sealed class Session
		{
			private static readonly int _bodyMagic = CharConstant.FromString("tag!");
			private static readonly int _layoutMagic = CharConstant.FromString("blay");
			private static readonly int _dataMagicTop = CharConstant.FromString("bdat");
			private static readonly int _blockMagic = CharConstant.FromString("tgbl");
			private static readonly int _structMagic = CharConstant.FromString("tgst");
			private static readonly int _stringIdMagic = CharConstant.FromString("tgsi");
			private static readonly int _dataMagic = CharConstant.FromString("tgda");
			private static readonly int _referenceMagic = CharConstant.FromString("tgrf");

			// "tg" + 'r' (attached) or NUL (detached) + "c" - see FifthGenFieldTypes.IsPageableResourceSection and
			// IsResourceAttached, which this is the inverse of. No pageable resource turned up in any of the five sample
			// tags, so this pairing is inferred from those two methods rather than checked against a real section.
			private const int PageableResourceAttachedMagic = 0x74677263;
			private const int PageableResourceDetachedMagic = 0x74670063;

			private readonly Endian _endianness;
			private readonly EndianReader _src;

			internal Session(FifthGenTagFile tagFile)
			{
				_endianness = tagFile.Endianness;
				_src = new EndianReader(new MemoryStream(tagFile.OriginalPayload, false), _endianness);
				Data = tagFile.Data;
			}

			private FifthGenTagBlock Data { get; }

			internal byte[] Run()
			{
				using (var destStream = new MemoryStream())
				{
					var dest = new EndianWriter(destStream, _endianness);

					_src.SeekTo(0);
					dest.WriteBlock(_src.ReadBlock(FifthGenTagHeader.Size));

					FifthGenChunk body = FifthGenChunk.ReadExpecting(_src, _src.Length, _bodyMagic, "the tag body");
					long sizePos = BeginChunk(dest, _bodyMagic, body.Version);
					long contentStart = dest.Position;

					_src.SeekTo(body.ContentOffset);
					while (_src.Position < body.ContentEnd)
					{
						FifthGenChunk child = FifthGenChunk.Read(_src, body.ContentEnd);
						if (child.Magic == _dataMagicTop)
							WriteBdatChunk(dest, child);
						else
							CopyChunkVerbatim(dest, child);
						_src.SeekTo(child.ContentEnd);
					}

					EndChunk(dest, sizePos, contentStart);

					// Never observed in any sample tag - the tag body's declared size always reaches exactly to the end
					// of the payload - but preserved rather than silently dropped if it ever does happen.
					if (body.ContentEnd < _src.Length)
					{
						_src.SeekTo(body.ContentEnd);
						dest.WriteBlock(_src.ReadBlock((int) (_src.Length - body.ContentEnd)));
					}

					return destStream.ToArray();
				}
			}

			/// <summary>
			///     Writes the <c>bdat</c> chunk. The schema-carrying <c>blay</c> chunk needs no counterpart of this: it is
			///     always copied verbatim by <see cref="Run" />, because nothing in this codebase mutates a tag's schema.
			/// </summary>
			private void WriteBdatChunk(EndianWriter dest, FifthGenChunk bdatChunk)
			{
				long sizePos = BeginChunk(dest, _dataMagicTop, bdatChunk.Version);
				long contentStart = dest.Position;

				_src.SeekTo(bdatChunk.ContentOffset);
				FifthGenChunk root = FifthGenChunk.ReadExpecting(_src, bdatChunk.ContentEnd, _blockMagic,
					"the payload's root block");
				WriteBlockSection(dest, root, Data);

				EndChunk(dest, sizePos, contentStart);
			}

			/// <summary>
			///     Writes one <c>tgbl</c> chunk: a block's count, flags, elements' packed data and - when
			///     <see cref="FifthGenTagBlock.HasElementSections" /> - one wrapper section per element.
			/// </summary>
			/// <param name="srcChunk">
			///     The block's original chunk, already read from <see cref="_src" /> with the stream left at its content
			///     offset, or <c>null</c> if this block has no original counterpart to fall back to (nested inside a struct
			///     field whose wrapper had to be freshly written - see <see cref="WriteStructFieldWrapper" />).
			/// </param>
			private void WriteBlockSection(EndianWriter dest, FifthGenChunk srcChunk, FifthGenTagBlock block)
			{
				if (srcChunk != null && !block.IsDirty)
				{
					CopyChunkVerbatim(dest, srcChunk);
					return;
				}

				long sizePos = BeginChunk(dest, _blockMagic, 0);
				long contentStart = dest.Position;

				dest.WriteUInt32(block.DeclaredCount);
				dest.WriteUInt32(block.Flags);

				foreach (FifthGenTagStruct element in block.Elements)
					WriteInlineStruct(dest, element);

				// The element list only still lines up with srcChunk's if nothing added, removed or reordered an
				// element - an individual element being dirty does not disturb this, only the list shape does.
				bool elementsLineUpWithSource = srcChunk != null && !block.ElementsDirty;
				int elementSize = block.ElementDefinition.InlineSize;

				if (block.HasElementSections)
				{
					if (elementsLineUpWithSource)
						_src.SeekTo(srcChunk.ContentOffset + 8 + (long) block.Elements.Count*elementSize);

					foreach (FifthGenTagStruct element in block.Elements)
					{
						FifthGenChunk wrapper = elementsLineUpWithSource
							? FifthGenChunk.ReadExpecting(_src, srcChunk.ContentEnd, _structMagic, "a block element")
							: null;
						WriteElementWrapper(dest, wrapper, element);
						if (wrapper != null)
							_src.SeekTo(wrapper.ContentEnd);
					}
				}

				EndChunk(dest, sizePos, contentStart);
			}

			/// <summary>
			///     Writes one struct instance's packed inline bytes: the concatenation of every field's
			///     <see cref="FifthGenTagValue.RawData" />, recursing into a struct or array field's own fields rather than
			///     trusting its own <see cref="FifthGenTagValue.RawData" /> (which is never kept in sync after an edit
			///     nested inside it - see the remarks on <see cref="FifthGenTagValue.RawData" />).
			/// </summary>
			/// <remarks>
			///     Never touches <see cref="_src" />. A field's inline bytes are always exactly its (possibly edited)
			///     <see cref="FifthGenTagValue.RawData" />, whether or not this particular struct instance is dirty, so
			///     there is nothing for the original bytes to add here - unlike the nested sections a block, struct,
			///     stringID, tag reference or data field contributes, inline bytes carry no information this writer cannot
			///     already get from the decoded model.
			/// </remarks>
			private static void WriteInlineStruct(EndianWriter dest, FifthGenTagStruct instance)
			{
				foreach (FifthGenTagValue value in instance.Values)
				{
					if (value is FifthGenStructValue structField)
						WriteInlineStruct(dest, structField.Value);
					else if (value is FifthGenArrayValue arrayField)
					{
						foreach (FifthGenTagStruct element in arrayField.Elements)
							WriteInlineStruct(dest, element);
					}
					else
						dest.WriteBlock(value.RawData);
				}
			}

			/// <summary>
			///     Writes a block element's own wrapper section. Unlike <see cref="WriteStructFieldWrapper" />, an element
			///     wrapper is never elided - <see cref="FifthGenTagDataReader" /> always reads one for every element
			///     <see cref="FifthGenTagBlock.HasElementSections" /> says has one - so there is no size-zero special case
			///     to consider here.
			/// </summary>
			private void WriteElementWrapper(EndianWriter dest, FifthGenChunk wrapper, FifthGenTagStruct element)
			{
				if (wrapper != null && !element.IsDirty)
				{
					CopyChunkVerbatim(dest, wrapper);
					return;
				}

				long sizePos = BeginChunk(dest, _structMagic, 0);
				long contentStart = dest.Position;

				if (wrapper != null)
					_src.SeekTo(wrapper.ContentOffset);
				WriteWrapperContent(dest, wrapper?.ContentEnd, element);

				EndTgstChunk(dest, sizePos, contentStart);
			}

			/// <summary>
			///     Writes a struct-typed field's wrapper section, preserving elision (see this class's remarks) whenever
			///     there are original bytes to check it against.
			/// </summary>
			private void WriteStructFieldWrapper(EndianWriter dest, FifthGenChunk wrapper, FifthGenTagStruct instance)
			{
				if (wrapper != null && !instance.IsDirty)
				{
					CopyChunkVerbatim(dest, wrapper);
					return;
				}

				long sizePos = BeginChunk(dest, _structMagic, 0);
				long contentStart = dest.Position;

				// A wrapper genuinely elided in the original carries no bytes to read fields' sections out of; if
				// something inside is dirty anyway, every field has to be written fresh rather than partially replayed
				// from a wrapper that was never populated in the first place.
				bool hasContent = wrapper != null && wrapper.Size > 0;
				if (hasContent)
					_src.SeekTo(wrapper.ContentOffset);
				WriteWrapperContent(dest, hasContent ? wrapper.ContentEnd : (long?) null, instance);

				EndTgstChunk(dest, sizePos, contentStart);
			}

			/// <summary>
			///     Writes the entire content of one wrapper - a struct instance's fields' sections followed by whatever the
			///     wrapper held beyond them (<see cref="FifthGenTagStruct.TrailingSections" />), which is never mutated by
			///     anything in this codebase and so is always copied verbatim from whatever is left of the source content
			///     when there is a source to read it from.
			/// </summary>
			/// <remarks>
			///     This is the one place the trailing content is read, precisely because it is the one place that knows
			///     where a wrapper's content truly ends: <see cref="WriteStructContentSections" /> is also used to write an
			///     array field's elements' sections inline into whatever wrapper contains the array, and those elements
			///     share that wrapper's content rather than each owning a bound of their own, so treating whatever followed
			///     an element's own fields as that element's trailing content would - and, before this split existed, did -
			///     consume everything up to the wrapper's real end after the first element instead of after all of them.
			/// </remarks>
			/// <param name="srcContentEnd">
			///     The offset in <see cref="_src" /> the wrapper's content ends at, with the stream currently positioned at
			///     its start; or <c>null</c> if this instance has no original wrapper content to fall back to, in which case
			///     every field is written fresh and there is no trailing content to speak of.
			/// </param>
			private void WriteWrapperContent(EndianWriter dest, long? srcContentEnd, FifthGenTagStruct instance)
			{
				WriteStructContentSections(dest, srcContentEnd, instance);

				if (srcContentEnd.HasValue && _src.Position < srcContentEnd.Value)
					dest.WriteBlock(_src.ReadBlock((int) (srcContentEnd.Value - _src.Position)));
			}

			/// <summary>
			///     Writes the nested sections a struct instance's fields contribute, in field order. An array field
			///     contributes no wrapper of its own - its elements' sections are written inline here, sharing whatever
			///     wrapper (or lack of one) the array field itself is in - which is why this recurses into
			///     <see cref="WriteStructContentSections" /> for an element rather than into <see cref="WriteWrapperContent" />.
			/// </summary>
			/// <param name="srcContentEnd">
			///     The offset in <see cref="_src" /> the enclosing wrapper's content ends at; or <c>null</c> if this instance
			///     has no original wrapper content to fall back to, in which case every field is written fresh.
			/// </param>
			private void WriteStructContentSections(EndianWriter dest, long? srcContentEnd, FifthGenTagStruct instance)
			{
				foreach (FifthGenTagValue value in instance.Values)
				{
					if (value is FifthGenBlockValue block)
					{
						FifthGenChunk section = ReadSectionOrNull(srcContentEnd, _blockMagic);
						WriteBlockSection(dest, section, block.Value);
						SeekPastOrNull(section);
					}
					else if (value is FifthGenStructValue structure)
					{
						FifthGenChunk section = ReadSectionOrNull(srcContentEnd, _structMagic);
						WriteStructFieldWrapper(dest, section, structure.Value);
						SeekPastOrNull(section);
					}
					else if (value is FifthGenArrayValue array)
					{
						foreach (FifthGenTagStruct element in array.Elements)
							WriteStructContentSections(dest, srcContentEnd, element);
					}
					else if (value is FifthGenStringIDValue stringId)
					{
						WriteLeafSection(dest, srcContentEnd, _stringIdMagic, stringId.Dirty,
							() => Encoding.UTF8.GetBytes(stringId.Value ?? string.Empty));
					}
					else if (value is FifthGenTagReferenceValue reference)
					{
						WriteLeafSection(dest, srcContentEnd, _referenceMagic, reference.Dirty,
							() => EncodeReferenceSection(reference));
					}
					else if (value is FifthGenDataValue data)
					{
						WriteLeafSection(dest, srcContentEnd, _dataMagic, data.Dirty, () => data.Contents);
					}
					else if (value is FifthGenResourceValue resource)
					{
						WriteResourceSection(dest, srcContentEnd, resource);
					}
				}
			}

			/// <summary>
			///     Writes one of the section shapes whose whole content is a single opaque byte buffer - a stringID's text,
			///     a tag reference's group and path, or a data field's buffer - copying it verbatim when there are original
			///     bytes and the field was not written through, or encoding it fresh otherwise.
			/// </summary>
			private void WriteLeafSection(EndianWriter dest, long? srcContentEnd, int magic, bool dirty,
				Func<byte[]> encodeFresh)
			{
				FifthGenChunk section = ReadSectionOrNull(srcContentEnd, magic);

				if (section != null && !dirty)
				{
					CopyChunkVerbatim(dest, section);
				}
				else
				{
					byte[] content = encodeFresh();
					long sizePos = BeginChunk(dest, magic, 0);
					long contentStart = dest.Position;
					dest.WriteBlock(content);
					EndChunk(dest, sizePos, contentStart);
				}

				SeekPastOrNull(section);
			}

			private void WriteResourceSection(EndianWriter dest, long? srcContentEnd, FifthGenResourceValue resource)
			{
				FifthGenChunk section = null;
				if (srcContentEnd.HasValue)
					section = FifthGenChunk.Read(_src, srcContentEnd.Value);

				if (section != null && !resource.Dirty)
				{
					CopyChunkVerbatim(dest, section);
				}
				else
				{
					int magic = resource.IsAttached ? PageableResourceAttachedMagic : PageableResourceDetachedMagic;
					long sizePos = BeginChunk(dest, magic, 0);
					long contentStart = dest.Position;
					dest.WriteBlock(resource.Contents);
					EndChunk(dest, sizePos, contentStart);
				}

				SeekPastOrNull(section);
			}

			/// <summary>
			///     Encodes a tag reference's section: empty for a null reference, otherwise the target group's four-CC
			///     (byte-reversed the same way every four-CC in this format is - see <see cref="FifthGenChunk.ReadMagic" />)
			///     followed by the path, with no terminator. See this class's remarks for why an unedited reference is
			///     never routed through here.
			/// </summary>
			private byte[] EncodeReferenceSection(FifthGenTagReferenceValue reference)
			{
				if (reference.IsNull)
					return Array.Empty<byte>();

				byte[] pathBytes = Encoding.UTF8.GetBytes(reference.Path);
				byte[] magicBytes = FifthGenTagValue.EncodeInteger(unchecked((uint) reference.GroupMagic), 4, _endianness);
				var content = new byte[4 + pathBytes.Length];
				Buffer.BlockCopy(magicBytes, 0, content, 0, 4);
				Buffer.BlockCopy(pathBytes, 0, content, 4, pathBytes.Length);
				return content;
			}

			private FifthGenChunk ReadSectionOrNull(long? srcContentEnd, int magic)
			{
				if (!srcContentEnd.HasValue)
					return null;
				return FifthGenChunk.ReadExpecting(_src, srcContentEnd.Value, magic, "a section");
			}

			private void SeekPastOrNull(FifthGenChunk section)
			{
				if (section != null)
					_src.SeekTo(section.ContentEnd);
			}

			private void CopyChunkVerbatim(EndianWriter dest, FifthGenChunk chunk)
			{
				_src.SeekTo(chunk.HeaderOffset);
				dest.WriteBlock(_src.ReadBlock(FifthGenChunk.HeaderSize + (int) chunk.Size));
			}

			private static long BeginChunk(EndianWriter dest, int magic, uint version)
			{
				dest.WriteUInt32(unchecked((uint) magic));
				dest.WriteUInt32(version);
				long sizePos = dest.Position;
				dest.WriteUInt32(0);
				return sizePos;
			}

			private static void EndChunk(EndianWriter dest, long sizePos, long contentStart)
			{
				long end = dest.Position;
				var size = (uint) (end - contentStart);
				dest.SeekTo(sizePos);
				dest.WriteUInt32(size);
				dest.SeekTo(end);
			}

			/// <summary>
			///     Ends a <c>tgst</c> chunk, writing its recomputed content size into both the size dword and the dword
			///     immediately before it - the one every other chunk shape uses for a real version number, but which for
			///     this one shape duplicates the size instead. See this class's remarks for how that was established.
			/// </summary>
			private static void EndTgstChunk(EndianWriter dest, long sizePos, long contentStart)
			{
				long end = dest.Position;
				var size = (uint) (end - contentStart);
				dest.SeekTo(sizePos - 4);
				dest.WriteUInt32(size);
				dest.WriteUInt32(size);
				dest.SeekTo(end);
			}
		}
	}
}
