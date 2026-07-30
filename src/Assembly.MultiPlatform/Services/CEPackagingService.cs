using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Blamite.Blam.FifthGen;
using Blamite.Blam.FifthGen.Structures;
using Blamite.IO.IoStore;
using Blamite.Serialization;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>Which step of an unpack or repack is currently running, for progress reporting.</summary>
	public enum CEPackagingStage
	{
		Mounting,
		Discovering,
		Copying,
		Validating,
		Writing,
		Done
	}

	/// <summary>One progress update from a running unpack or repack, meant to drive a determinate progress bar and a "what it is doing now" line.</summary>
	public sealed class CEPackagingProgress
	{
		public CEPackagingStage Stage { get; init; }
		public string Detail { get; init; } = "";
		public int Completed { get; init; }
		public int Total { get; init; }
	}

	/// <summary>One tag this unpack found, for the preview list shown before anything is written.</summary>
	public sealed record CEUnpackTagPreview(string Name, string Group, long PayloadSize);

	/// <summary>What an unpack would do, computed without writing anything, for the confirmation dialog.</summary>
	public sealed record CEUnpackPreview(
		string SourceUtoc,
		string MountDirectory,
		int ContainerCount,
		IReadOnlyList<CEUnpackTagPreview> Tags,
		long TotalTagBytes,
		IReadOnlyList<string> MountWarnings);

	/// <summary>What an unpack actually did.</summary>
	public sealed record CEUnpackResult(
		bool Success,
		string? Error,
		string OutputDirectory,
		int TagsWritten,
		long TagBytesWritten,
		int ChunksWritten,
		IReadOnlyList<string> Warnings,
		TimeSpan Elapsed);

	/// <summary>
	///     The disposition <see cref="CEPackagingService.PreviewRepack" /> found for one <c>.ubulk</c> file under a
	///     candidate tags directory.
	/// </summary>
	public sealed class CERepackFileStatus
	{
		public string FilePath { get; init; } = "";
		public string LogicalName { get; init; } = "";
		public string Group { get; init; } = "";
		public bool MatchedExistingTag { get; init; }
		public bool ParsesCleanly { get; init; }
		public string? ParseProblem { get; init; }
		public bool IdenticalToSource { get; init; }
		public long FileSize { get; init; }
	}

	/// <summary>What a repack would do, computed without writing anything, for the confirmation dialog.</summary>
	public sealed class CERepackPreview
	{
		public string SourceDirectory { get; init; } = "";
		public string TagsDirectory { get; init; } = "";
		public IReadOnlyList<string> ContainerFiles { get; init; } = Array.Empty<string>();
		public IReadOnlyList<CERepackFileStatus> TagFiles { get; init; } = Array.Empty<CERepackFileStatus>();
		public int UntouchedSourceTagCount { get; init; }
		public IReadOnlyList<string> MountWarnings { get; init; } = Array.Empty<string>();

		public int MatchedCount => TagFiles.Count(f => f.MatchedExistingTag);
		public int ChangedCount => TagFiles.Count(f => f.MatchedExistingTag && f.ParsesCleanly && !f.IdenticalToSource);
		public int UnmatchedCount => TagFiles.Count(f => !f.MatchedExistingTag);
		public int InvalidCount => TagFiles.Count(f => f.MatchedExistingTag && !f.ParsesCleanly);

		/// <summary>
		///     Gets whether <see cref="CEPackagingService.Repack" /> can be run at all against this preview. A file that
		///     matched a known tag but fails to parse as a Blam tag payload blocks the whole run - see this class's
		///     remarks on <see cref="CERepackFileStatus.ParseProblem" /> for why partial corruption is worse than refusing.
		/// </summary>
		public bool CanProceed => InvalidCount == 0;
	}

	/// <summary>What a repack actually did.</summary>
	public sealed record CERepackResult(
		bool Success,
		string? Error,
		string OutputDirectory,
		int FilesCopied,
		int TagsChanged,
		int TagsUnchanged,
		IReadOnlyList<string> ChangedContainers,
		IReadOnlyList<string> Warnings,
		TimeSpan Elapsed)
	{
		/// <summary>
		///     Gets whether the output directory is byte-identical to the source: true exactly when nothing this run
		///     touched actually differed from what was already on disk, so every container was a plain file copy and
		///     <see cref="IoStoreContainerWriter" /> was never invoked at all.
		/// </summary>
		public bool ByteIdenticalToSource => Success && TagsChanged == 0;
	}

	/// <summary>
	///     Unpacks a Campaign Evolved container set to a directory of tag files, and repacks a directory of tag files
	///     back into a copy of the container set they came from.
	/// </summary>
	/// <remarks>
	///     <para>
	///         This deliberately does not attempt to rebuild a container from nothing. <see cref="Repack" /> always
	///         starts from a real, on-disk source container set - the one <see cref="Unpack" /> was pointed at, or an
	///         identical copy of it - and asks <see cref="IoStoreContainerWriter" /> (already proven against the real
	///         mod containers used to develop it) to substitute new bytes for one chunk at a time. The alternative -
	///         reconstructing a <c>.utoc</c>/<c>.ucas</c> pair purely from a directory of loose <c>.ubulk</c> files and
	///         a hand-rolled manifest - would mean re-deriving every non-tag chunk (<c>ExportBundleData</c>,
	///         <c>ContainerHeader</c>) and every table-of-contents field this codebase does not otherwise write, none of
	///         which has been checked against real data the way chunk substitution has. "Patch a real container" is the
	///         narrower claim, and it is the one this class can actually stand behind.
	///     </para>
	///     <para>
	///         Every tag written to disk by <see cref="Unpack" /> is named <c>&lt;path&gt;.&lt;group&gt;.ubulk</c>,
	///         where <c>&lt;path&gt;</c> is the tag's resolved name (with <c>/</c> becoming real subdirectories) and
	///         <c>&lt;group&gt;</c> is its four-CC. <see cref="Repack" /> parses that same shape back out of a
	///         filename to know which tag a file is meant to replace; a file whose name does not end in
	///         <c>.&lt;fourCC&gt;.ubulk</c> is ignored rather than guessed at.
	///     </para>
	/// </remarks>
	public static class CEPackagingService
	{
		private const string TagFileExtension = ".ubulk";

		/// <summary>Determines whether a path names, or directly contains, at least one <c>.utoc</c> file.</summary>
		public static bool LooksLikeContainerSet(string path)
		{
			try
			{
				if (File.Exists(path))
					return string.Equals(Path.GetExtension(path), ".utoc", StringComparison.OrdinalIgnoreCase);
				return Directory.Exists(path) && Directory.EnumerateFiles(path, "*.utoc").Any();
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		// ---- unpack ----

		public static CEUnpackPreview PreviewUnpack(string sourcePath, EngineDatabase db)
		{
			string anyUtoc = ResolveAnyUtoc(sourcePath);
			CacheSession session = CacheSession.Open(anyUtoc, db);
			try
			{
				RequireFifthGeneration(session);
				var table = (FifthGenTagTable) session.Cache.Tags;

				var tags = new List<CEUnpackTagPreview>();
				long totalBytes = 0;
				foreach (TagGroupInfo group in session.Groups)
				{
					foreach (TagInfo tag in group.Tags)
					{
						var raw = (FifthGenTag) tag.Raw;
						tags.Add(new CEUnpackTagPreview(tag.Name, group.Magic, raw.RawPayload.Length));
						totalBytes += raw.RawPayload.Length;
					}
				}

				return new CEUnpackPreview(anyUtoc, table.MountedContainers.Count > 0 ? Path.GetDirectoryName(anyUtoc) ?? sourcePath : sourcePath,
					table.MountedContainers.Count, tags, totalBytes, table.Warnings.ToList());
			}
			finally
			{
				session.Dispose();
			}
		}

		/// <summary>
		///     Extracts every tag a container set resolves to into a directory tree of <c>.ubulk</c> files, one per
		///     tag, named by its resolved path and group. This is the "plain" output the brief for this feature calls
		///     out as the default: only the tag payloads a modder would actually open, nothing else.
		/// </summary>
		/// <param name="sourcePath">A <c>.utoc</c> file, or a folder directly holding one or more of them.</param>
		/// <param name="outputDirectory">
		///     Where to write the <c>tags/</c> tree. Created if it does not exist. Never the same directory as
		///     <paramref name="sourcePath" /> resolves to - refused outright rather than risking a modder's own
		///     container set being shadowed by its own unpack.
		/// </param>
		/// <param name="includeRawChunks">
		///     When true, additionally dumps every chunk of every mounted container - tag payloads included, a second
		///     time, alongside every chunk this codebase does not interpret - into <c>containers/</c>, verbatim and
		///     unlabelled beyond its index and <see cref="IoChunkType" />. This is the "everything" mode: a raw,
		///     honest capture for diffing two versions of a mod against each other, not something <see cref="Repack" />
		///     reads back (see this class's remarks).
		/// </param>
		public static CEUnpackResult Unpack(string sourcePath, string outputDirectory, EngineDatabase db, bool includeRawChunks,
			IProgress<CEPackagingProgress>? progress, CancellationToken ct)
		{
			var started = DateTime.UtcNow;
			string anyUtoc = ResolveAnyUtoc(sourcePath);
			string mountDirectory = Path.GetDirectoryName(Path.GetFullPath(anyUtoc)) ?? sourcePath;

			if (PathsRefersToSameDirectory(mountDirectory, outputDirectory))
			{
				return new CEUnpackResult(false,
					$"The output directory is the same as the source container set (\"{mountDirectory}\"). Choose a different folder.",
					outputDirectory, 0, 0, 0, Array.Empty<string>(), DateTime.UtcNow - started);
			}

			progress?.Report(new CEPackagingProgress { Stage = CEPackagingStage.Mounting, Detail = "Mounting container set..." });

			CacheSession session = CacheSession.Open(anyUtoc, db);
			try
			{
				RequireFifthGeneration(session);
				ct.ThrowIfCancellationRequested();

				var table = (FifthGenTagTable) session.Cache.Tags;
				var warnings = new List<string>(table.Warnings);

				string tagsRoot = Path.Combine(outputDirectory, "tags");
				Directory.CreateDirectory(tagsRoot);

				var allTags = session.Groups.SelectMany(g => g.Tags).ToList();
				int written = 0;
				long bytesWritten = 0;

				for (var i = 0; i < allTags.Count; i++)
				{
					ct.ThrowIfCancellationRequested();
					TagInfo tag = allTags[i];
					var raw = (FifthGenTag) tag.Raw;
					progress?.Report(new CEPackagingProgress
					{
						Stage = CEPackagingStage.Writing, Detail = $"{tag.Name}.{tag.Group}", Completed = i, Total = allTags.Count
					});

					string path = TagFilePath(tagsRoot, tag.Name, tag.Group);
					Directory.CreateDirectory(Path.GetDirectoryName(path)!);
					File.WriteAllBytes(path, raw.RawPayload);
					written++;
					bytesWritten += raw.RawPayload.Length;
				}

				int chunksWritten = 0;
				if (includeRawChunks)
				{
					string containersRoot = Path.Combine(outputDirectory, "containers");
					Directory.CreateDirectory(containersRoot);

					IList<FifthGenMountedContainer> mounted = table.MountedContainers;
					for (var c = 0; c < mounted.Count; c++)
					{
						ct.ThrowIfCancellationRequested();
						FifthGenMountedContainer container = mounted[c];
						IoStoreTableOfContents toc = container.Container.TableOfContents;
						string stem = Path.GetFileNameWithoutExtension(container.TocPath);
						string containerDir = Path.Combine(containersRoot, SanitizeSegment(stem));
						Directory.CreateDirectory(containerDir);

						for (var chunkIndex = 0; chunkIndex < toc.ChunkIds.Count; chunkIndex++)
						{
							ct.ThrowIfCancellationRequested();
							progress?.Report(new CEPackagingProgress
							{
								Stage = CEPackagingStage.Writing,
								Detail = $"{stem} chunk {chunkIndex}/{toc.ChunkIds.Count} ({toc.ChunkIds[chunkIndex].Type})",
								Completed = c,
								Total = mounted.Count
							});

							byte[] content;
							try
							{
								content = container.Container.ReadChunk(chunkIndex);
							}
							catch (IoStoreException ex)
							{
								warnings.Add($"\"{stem}\" chunk {chunkIndex}: could not read for the raw dump: {ex.Message}");
								continue;
							}

							string chunkPath = Path.Combine(containerDir, $"{chunkIndex}-{toc.ChunkIds[chunkIndex].Type}.bin");
							File.WriteAllBytes(chunkPath, content);
							chunksWritten++;
						}
					}
				}

				progress?.Report(new CEPackagingProgress { Stage = CEPackagingStage.Done, Detail = "Done.", Completed = allTags.Count, Total = allTags.Count });
				return new CEUnpackResult(true, null, outputDirectory, written, bytesWritten, chunksWritten, warnings, DateTime.UtcNow - started);
			}
			finally
			{
				session.Dispose();
			}
		}

		// ---- repack ----

		/// <summary>
		///     Matches every <c>.ubulk</c> file found under a tags directory against the tags a source container set
		///     currently resolves to, and validates each match by parsing it as a <see cref="FifthGenTagFile" /> -
		///     without writing anything. See <see cref="CERepackPreview.CanProceed" /> for what a caller should check
		///     before calling <see cref="Repack" /> with the same arguments.
		/// </summary>
		public static CERepackPreview PreviewRepack(string sourcePath, string tagsDirectory, EngineDatabase db)
		{
			string anyUtoc = ResolveAnyUtoc(sourcePath);
			string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(anyUtoc)) ?? sourcePath;

			CacheSession session = CacheSession.Open(anyUtoc, db);
			try
			{
				RequireFifthGeneration(session);
				var table = (FifthGenTagTable) session.Cache.Tags;

				Dictionary<string, TagInfo> byKey = BuildTagLookup(session);
				var matchedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				var statuses = new List<CERepackFileStatus>();
				foreach (string file in EnumerateTagFiles(tagsDirectory))
				{
					if (!TryParseTagFileName(tagsDirectory, file, out string logicalName, out string group))
						continue;

					string key = TagKey(logicalName, group);
					byte[] bytes = File.ReadAllBytes(file);

					bool matched = byKey.TryGetValue(key, out TagInfo? existing);
					bool identical = matched && ((FifthGenTag) existing!.Raw).RawPayload.AsSpan().SequenceEqual(bytes);
					string? problem = null;
					bool parses = FifthGenTagFile.IsTagPayload(bytes);
					if (!parses)
					{
						problem = "No BLAM/MALB tag signature at offset 0x3C - this is not a Blam tag payload.";
					}
					else
					{
						try
						{
							_ = new FifthGenTagFile(bytes);
						}
						catch (Exception ex)
						{
							parses = false;
							problem = $"{ex.GetType().Name}: {ex.Message}";
						}
					}

					if (matched) matchedKeys.Add(key);
					statuses.Add(new CERepackFileStatus
					{
						FilePath = file,
						LogicalName = logicalName,
						Group = group,
						MatchedExistingTag = matched,
						ParsesCleanly = parses,
						ParseProblem = problem,
						IdenticalToSource = identical,
						FileSize = bytes.LongLength
					});
				}

				int untouched = byKey.Keys.Count(k => !matchedKeys.Contains(k));
				string[] containerFiles = Directory.Exists(sourceDirectory)
					? Directory.GetFiles(sourceDirectory).Select(Path.GetFileName).Where(n => n != null).Select(n => n!).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray()
					: Array.Empty<string>();

				return new CERepackPreview
				{
					SourceDirectory = sourceDirectory,
					TagsDirectory = tagsDirectory,
					ContainerFiles = containerFiles,
					TagFiles = statuses,
					UntouchedSourceTagCount = untouched,
					MountWarnings = table.Warnings.ToList()
				};
			}
			finally
			{
				session.Dispose();
			}
		}

		/// <summary>
		///     Copies a source container set to <paramref name="outputDirectory" /> and substitutes the content of
		///     every matched, changed, validly-parsing tag file under <paramref name="tagsDirectory" /> into its
		///     owning container, via <see cref="IoStoreContainerWriter.ReplaceChunk" />.
		/// </summary>
		/// <remarks>
		///     Every candidate file is parsed and checked before a single byte is written anywhere - see
		///     <see cref="CERepackPreview.CanProceed" />. A file that fails that check aborts the whole run rather than
		///     being skipped, because writing everything else and silently dropping the one tag that did not parse
		///     would hand back a container set that looks complete and is not; refusing outright is the honest failure
		///     mode here, not a partially-applied one.
		/// </remarks>
		public static CERepackResult Repack(string sourcePath, string tagsDirectory, string outputDirectory, EngineDatabase db,
			IProgress<CEPackagingProgress>? progress, CancellationToken ct)
		{
			var started = DateTime.UtcNow;
			CERepackPreview preview = PreviewRepack(sourcePath, tagsDirectory, db);

			if (PathsRefersToSameDirectory(preview.SourceDirectory, outputDirectory))
			{
				return new CERepackResult(false,
					$"The output directory is the same as the source container set (\"{preview.SourceDirectory}\"). Repack always writes to a separate folder so the source is never touched.",
					outputDirectory, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow - started);
			}

			if (!preview.CanProceed)
			{
				CERepackFileStatus bad = preview.TagFiles.First(f => f.MatchedExistingTag && !f.ParsesCleanly);
				return new CERepackResult(false,
					$"\"{Path.GetFileName(bad.FilePath)}\" (matched {bad.LogicalName}.{bad.Group}) does not parse as a valid tag: {bad.ParseProblem}. " +
					"Nothing was written; fix or remove this file and try again.",
					outputDirectory, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow - started);
			}

			ct.ThrowIfCancellationRequested();

			string anyUtoc = ResolveAnyUtoc(sourcePath);
			CacheSession session = CacheSession.Open(anyUtoc, db);
			List<(string DestTocPath, IoChunkId ChunkId, byte[] NewBytes, string TagLabel)> pending;
			StructureLayoutCollection layouts;
			try
			{
				RequireFifthGeneration(session);
				layouts = session.Engine.Layouts;

				Dictionary<string, TagInfo> byKey = BuildTagLookup(session);
				pending = new List<(string, IoChunkId, byte[], string)>();

				foreach (CERepackFileStatus status in preview.TagFiles)
				{
					if (!status.MatchedExistingTag || status.IdenticalToSource) continue;

					TagInfo tag = byKey[TagKey(status.LogicalName, status.Group)];
					var raw = (FifthGenTag) tag.Raw;
					IoChunkId chunkId = FindBulkDataChunkId(raw.Container.Container.TableOfContents, raw.PackageId.Value);
					string destToc = Path.Combine(outputDirectory, Path.GetFileName(raw.Container.TocPath));
					pending.Add((destToc, chunkId, File.ReadAllBytes(status.FilePath), $"{status.LogicalName}.{status.Group}"));
				}
			}
			finally
			{
				// Closed before the copy phase below, which recreates every file the source mount has open -
				// on a platform that enforces exclusive file locks, a stale read handle here would make the
				// upcoming File.Copy calls (harmless, since they only ever target the *destination*) look safe
				// while leaving nothing to actually rewrite once IoStoreContainerWriter runs. Explicit and
				// unconditional rather than relying on a using block's ordering staying obvious as this method grows.
				session.Dispose();
			}

			ct.ThrowIfCancellationRequested();
			Directory.CreateDirectory(outputDirectory);
			int filesCopied = CopyDirectory(preview.SourceDirectory, outputDirectory, progress, ct);

			var changedContainers = new List<string>();
			for (var i = 0; i < pending.Count; i++)
			{
				ct.ThrowIfCancellationRequested();
				(string destToc, IoChunkId chunkId, byte[] newBytes, string tagLabel) = pending[i];
				progress?.Report(new CEPackagingProgress
				{
					Stage = CEPackagingStage.Writing, Detail = $"Writing {tagLabel} into {Path.GetFileName(destToc)}", Completed = i, Total = pending.Count
				});

				IoStoreContainerWriter.ReplaceChunk(destToc, layouts, chunkId, newBytes);
				changedContainers.Add(Path.GetFileName(destToc));
			}

			progress?.Report(new CEPackagingProgress { Stage = CEPackagingStage.Done, Detail = "Done.", Completed = pending.Count, Total = pending.Count });

			int unchanged = preview.MatchedCount - pending.Count;
			return new CERepackResult(true, null, outputDirectory, filesCopied, pending.Count, unchanged,
				changedContainers, preview.MountWarnings, DateTime.UtcNow - started);
		}

		// ---- shared helpers ----

		private static void RequireFifthGeneration(CacheSession session)
		{
			if (session.Engine.Engine != Blamite.Blam.EngineType.FifthGeneration)
			{
				throw new InvalidOperationException(
					$"\"{session.FilePath}\" is a {session.Engine.Engine} cache, not a Campaign Evolved container set. " +
					"Unpack/repack only understands fifth-generation IoStore containers.");
			}
		}

		private static string ResolveAnyUtoc(string sourcePath)
		{
			if (File.Exists(sourcePath) && string.Equals(Path.GetExtension(sourcePath), ".utoc", StringComparison.OrdinalIgnoreCase))
				return sourcePath;

			string directory = Directory.Exists(sourcePath) ? sourcePath : Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? sourcePath;
			string? first = Directory.EnumerateFiles(directory, "*.utoc", SearchOption.TopDirectoryOnly)
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
				.FirstOrDefault();

			if (first == null)
			{
				throw new InvalidOperationException(
					$"\"{sourcePath}\" has no .utoc files - this is not a Campaign Evolved container set.");
			}
			return first;
		}

		private static bool PathsRefersToSameDirectory(string a, string b)
		{
			try
			{
				string fa = Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string fb = Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				return string.Equals(fa, fb, StringComparison.OrdinalIgnoreCase);
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		private static Dictionary<string, TagInfo> BuildTagLookup(CacheSession session)
		{
			var result = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (TagGroupInfo group in session.Groups)
				foreach (TagInfo tag in group.Tags)
					result[TagKey(tag.Name, group.Magic)] = tag;
			return result;
		}

		private static string TagKey(string name, string group) => name + "." + group;

		private static string TagFilePath(string tagsRoot, string tagName, string group)
		{
			string[] segments = tagName.Replace('\\', '/').Split('/');
			string[] sanitized = segments.Select(SanitizeSegment).ToArray();
			sanitized[^1] = sanitized[^1] + "." + group + TagFileExtension;
			return Path.Combine(new[] { tagsRoot }.Concat(sanitized).ToArray());
		}

		private static string SanitizeSegment(string segment)
		{
			char[] invalid = Path.GetInvalidFileNameChars();
			char[] chars = segment.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray();
			string result = new string(chars).Trim();
			return result.Length == 0 ? "_" : result;
		}

		private static IEnumerable<string> EnumerateTagFiles(string tagsDirectory)
		{
			if (!Directory.Exists(tagsDirectory))
				return Array.Empty<string>();
			return Directory.EnumerateFiles(tagsDirectory, "*" + TagFileExtension, SearchOption.AllDirectories)
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		///     Recovers a tag's logical name and group from a path <see cref="Unpack" /> wrote (or one a caller built
		///     by hand the same way): the last two dot-separated segments of the file name are the group four-CC and
		///     the <c>.ubulk</c> extension, and everything from <paramref name="tagsRoot" /> up to there, slashes
		///     included, is the name.
		/// </summary>
		private static bool TryParseTagFileName(string tagsRoot, string filePath, out string logicalName, out string group)
		{
			logicalName = "";
			group = "";

			string fileName = Path.GetFileName(filePath);
			if (!fileName.EndsWith(TagFileExtension, StringComparison.OrdinalIgnoreCase))
				return false;

			string withoutExtension = fileName.Substring(0, fileName.Length - TagFileExtension.Length);
			int lastDot = withoutExtension.LastIndexOf('.');
			if (lastDot < 0 || lastDot == withoutExtension.Length - 1)
				return false;

			string candidateGroup = withoutExtension.Substring(lastDot + 1);
			if (candidateGroup.Length != 4)
				return false;

			string namePart = withoutExtension.Substring(0, lastDot);
			string relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(tagsRoot, filePath)) ?? "";
			string combined = relativeDirectory.Length > 0
				? relativeDirectory.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/') + "/" + namePart
				: namePart;

			logicalName = combined;
			group = candidateGroup;
			return true;
		}

		/// <summary>
		///     Finds the <c>BulkData</c> chunk sibling to a package's tag data, the same way
		///     <see cref="FifthGenCacheFile" />'s own save path does - duplicated rather than reused because that
		///     lookup is <c>private</c> there and this is a five-line linear scan over public
		///     <see cref="IoStoreTableOfContents" /> state, not a second implementation of anything this codebase
		///     already exposes.
		/// </summary>
		private static IoChunkId FindBulkDataChunkId(IoStoreTableOfContents toc, ulong packageId)
		{
			foreach (IoChunkId id in toc.ChunkIds)
			{
				if (id.PackageId == packageId && id.Type == IoChunkType.BulkData)
					return id;
			}
			throw new InvalidOperationException($"No BulkData chunk found for package 0x{packageId:X16}.");
		}

		private static int CopyDirectory(string sourceDirectory, string destinationDirectory, IProgress<CEPackagingProgress>? progress, CancellationToken ct)
		{
			string[] files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
			for (var i = 0; i < files.Length; i++)
			{
				ct.ThrowIfCancellationRequested();
				string relative = Path.GetRelativePath(sourceDirectory, files[i]);
				string destination = Path.Combine(destinationDirectory, relative);
				Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

				progress?.Report(new CEPackagingProgress { Stage = CEPackagingStage.Copying, Detail = relative, Completed = i, Total = files.Length });
				File.Copy(files[i], destination, overwrite: true);
			}
			return files.Length;
		}
	}
}
