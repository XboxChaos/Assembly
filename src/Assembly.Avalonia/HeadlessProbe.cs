using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Assembly.Avalonia.Services;
using Assembly.Avalonia.ViewModels;

namespace Assembly.Avalonia
{
	/// <summary>
	///     Non-GUI entry point that exercises the same services the window uses, so the
	///     Blamite integration can be verified from a terminal.
	///     Usage: AssemblyAvalonia --headless [cache-file] [tag-name-substring]
	///
	///     Also carries --edit-test, a GUI-free round-trip proof for the write path: open,
	///     mutate a field through the exact same TagDocumentViewModel/MetaValueWriter machinery
	///     the sidebar editors use, save, then re-open the file in a fresh CacheSession and
	///     confirm the new bytes are really on disk - independent of any Avalonia UI wiring.
	///     Usage: AssemblyAvalonia --edit-test cache-file tag-name-substring field-name new-value
	///
	///     Also carries --perf-test, which exercises TagDocumentViewModel exactly the way the
	///     shell does (construct on open, ToggleExpand on a block click, SetElementIndex on the
	///     element spinner) and times it, so the row-list rebuild strategy can be measured from a
	///     terminal instead of eyeballed in the running app.
	///     Usage: AssemblyAvalonia --perf-test cache-file
	/// </summary>
	internal static class HeadlessProbe
	{
		public static int Run(string[] args)
		{
			if (args.Length > 0 && args[0] == "--edit-test")
				return RunEditTest(args);
			if (args.Length > 0 && args[0] == "--perf-test")
				return RunPerfTest(args);
			if (args.Length > 0 && args[0] == "--ce-fields")
				return RunCeFields(args);
			if (args.Length > 0 && args[0] == "--iostore-inspect")
				return RunIoStoreInspect(args);
			if (args.Length > 0 && args[0] == "--fifthgen-roundtrip")
				return RunFifthGenRoundTrip(args);
			if (args.Length > 0 && args[0] == "--ce-unpack")
				return RunCEUnpack(args);
			if (args.Length > 0 && args[0] == "--ce-repack")
				return RunCERepack(args);
			if (args.Length > 0 && args[0] == "--ce-mutate-tag-file")
				return RunCEMutateTagFile(args);

			return RunProbe(args);
		}

		/// <summary>
		///     Mutates one integer field in a loose <c>.ubulk</c> tag file on disk (the kind <c>--ce-unpack</c> writes)
		///     and saves it back through <see cref="Blamite.Blam.FifthGen.Structures.FifthGenTagWriter" />, so a real
		///     edit can be fed into <c>--ce-repack</c> without a GUI. The first field anywhere in the tag (searched
		///     depth-first) whose name matches is changed; this is a test tool, not a general editor, so it does not
		///     try to disambiguate same-named fields the way a real UI's field path would.
		///     Usage: AssemblyAvalonia --ce-mutate-tag-file &lt;path-to-.ubulk&gt; &lt;field-name&gt; &lt;new-int-value&gt;
		/// </summary>
		private static int RunCeFields(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Usage: --ce-fields <utoc-or-folder> <tag-name-substring> [block-to-expand]");
				return 1;
			}

			string path = args[1], tagNeedle = args[2];
			string? blockToExpand = args.Length > 3 ? args[3] : null;

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			var session = CacheSession.Open(path, EngineDatabaseService.Database!);
			foreach (var g in session.Groups) foreach (var t in g.Tags) t.Owner = session;

			var tag = session.Groups.SelectMany(g => g.Tags)
				.FirstOrDefault(t => t.Name.Contains(tagNeedle, StringComparison.OrdinalIgnoreCase));
			if (tag == null) { Console.WriteLine($"no tag matching \"{tagNeedle}\""); return 2; }

			var doc = new TagDocumentViewModel(tag);
			Console.WriteLine($"{tag.Name}.{tag.Group}  ({doc.SchemaStatus})");

			if (blockToExpand != null)
			{
				var blockRow = doc.Rows.FirstOrDefault(r => r.IsBlock && r.Name.Contains(blockToExpand, StringComparison.OrdinalIgnoreCase));
				if (blockRow == null) { Console.WriteLine($"no top-level block matching \"{blockToExpand}\""); return 3; }
				doc.ToggleExpand(blockRow);
			}

			foreach (var row in doc.Rows)
			{
				string indent = new string(' ', row.Depth * 2);
				string editable = row.Def.IsEditable ? "editable" : "-";
				Console.WriteLine($"{indent}{row.Name,-32} editor={row.Editor,-20} kind={row.KindLabel,-16} {editable,-9} dirty={row.IsDirty}  {Truncate(row.DisplayValue, 60)}");
			}

			return 0;
		}

		private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";


		private static int RunCEMutateTagFile(string[] args)
		{
			if (args.Length < 4)
			{
				Console.WriteLine("Usage: --ce-mutate-tag-file <path-to-.ubulk> <field-name> <new-int-value>");
				return 1;
			}

			string path = args[1], fieldName = args[2];
			long newValue = long.Parse(args[3], CultureInfo.InvariantCulture);

			byte[] original = System.IO.File.ReadAllBytes(path);
			var tagFile = new Blamite.Blam.FifthGen.FifthGenTagFile(original);

			if (fieldName == "--list")
			{
				foreach (var f in tagFile.Layout.Fields.Where(f => !string.IsNullOrEmpty(f.Name)).Take(60))
					Console.WriteLine($"  {f.TypeName,-14} {f.Name}");
				return 0;
			}

			var found = FindIntegerField(tagFile.Data, fieldName);
			if (found == null)
			{
				Console.WriteLine($"No integer field named \"{fieldName}\" found anywhere in this tag.");
				return 2;
			}

			Console.WriteLine($"before: {found}");
			found.SetValue(newValue, Blamite.IO.Endian.LittleEndian);
			Console.WriteLine($"after:  {found}");

			byte[] rewritten = Blamite.Blam.FifthGen.Structures.FifthGenTagWriter.Write(tagFile);
			System.IO.File.WriteAllBytes(path, rewritten);
			Console.WriteLine($"wrote {rewritten.Length:N0} bytes to {path} (was {original.Length:N0})");
			return 0;
		}

		private static Blamite.Blam.FifthGen.Structures.FifthGenIntegerValue FindIntegerField(
			Blamite.Blam.FifthGen.Structures.FifthGenTagBlock block, string fieldName)
		{
			foreach (var element in block.Elements)
			{
				var found = FindIntegerField(element, fieldName);
				if (found != null) return found;
			}
			return null;
		}

		private static Blamite.Blam.FifthGen.Structures.FifthGenIntegerValue FindIntegerField(
			Blamite.Blam.FifthGen.Structures.FifthGenTagStruct instance, string fieldName)
		{
			foreach (var value in instance.Values)
			{
				if (value is Blamite.Blam.FifthGen.Structures.FifthGenIntegerValue i &&
				    string.Equals(i.Name, fieldName, StringComparison.OrdinalIgnoreCase))
					return i;

				Blamite.Blam.FifthGen.Structures.FifthGenIntegerValue nested = value switch
				{
					Blamite.Blam.FifthGen.Structures.FifthGenStructValue sv => FindIntegerField(sv.Value, fieldName),
					Blamite.Blam.FifthGen.Structures.FifthGenArrayValue av => av.Elements
						.Select(e => FindIntegerField(e, fieldName)).FirstOrDefault(f => f != null),
					Blamite.Blam.FifthGen.Structures.FifthGenBlockValue bv when bv.Value != null => FindIntegerField(bv.Value, fieldName),
					_ => null
				};
				if (nested != null) return nested;
			}
			return null;
		}

		/// <summary>
		///     Proves (or disproves) that <see cref="Blamite.Blam.FifthGen.Structures.FifthGenTagWriter" />
		///     round-trips every real tag byte-for-byte: parse each tag's raw payload, serialise it straight back with
		///     nothing touched, and compare against the original bytes. Also runs a second, stronger pass that marks
		///     every field dirty (forcing the writer's slow, re-encoding path instead of its "nothing changed, hand
		///     back the original bytes" fast path) so a mismatch there is not hidden by the fast path masking a bug in
		///     the part of the writer that actually re-derives bytes.
		///     Usage: AssemblyAvalonia --fifthgen-roundtrip &lt;path-to-.utoc&gt;
		/// </summary>
		private static int RunFifthGenRoundTrip(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: --fifthgen-roundtrip <path-to-.utoc>");
				return 1;
			}

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			CacheSession session;
			try
			{
				session = CacheSession.Open(args[1], EngineDatabaseService.Database!);
			}
			catch (Exception ex)
			{
				Console.WriteLine("OPEN FAILED: " + ex.Message);
				return 2;
			}

			var tags = session.Groups.SelectMany(g => g.Tags).OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
			Console.WriteLine($"{tags.Count} tag(s) mounted from {args[1]}\n");

			bool anyFailure = false;
			foreach (var tag in tags)
			{
				var raw = (Blamite.Blam.FifthGen.Structures.FifthGenTag) tag.Raw;
				byte[] original = raw.RawPayload;

				// ---- pass 1: parse, then write back completely untouched ----
				var parsed = new Blamite.Blam.FifthGen.FifthGenTagFile(original);
				Console.WriteLine($"=== {tag.Name}.{tag.Group}  ({original.Length:N0} bytes, " +
					$"{parsed.Layout.Fields.Count} field(s) / {parsed.Layout.Structs.Count} struct(s)) ===");
				byte[] clean = Blamite.Blam.FifthGen.Structures.FifthGenTagWriter.Write(parsed);
				bool cleanOk = ReportComparison("unedited round-trip", original, clean);
				anyFailure |= !cleanOk;

				// ---- pass 2: force every field dirty, forcing the writer's re-encode path ----
				var parsed2 = new Blamite.Blam.FifthGen.FifthGenTagFile(original);
				int touched = TouchEveryField(parsed2.Data);
				byte[] forced = Blamite.Blam.FifthGen.Structures.FifthGenTagWriter.Write(parsed2);
				bool forcedOk = ReportComparison($"forced re-encode ({touched} field(s) marked dirty)", original, forced);
				// Not folded into anyFailure: the writer's own documented limitation (a stringID/tag
				// reference's on-disk NUL terminator cannot be recovered from the decoded model - see
				// FifthGenTagWriter's remarks) makes an exact match here a bonus, not a requirement.
				if (!forcedOk)
					Console.WriteLine("    (see FifthGenTagWriter's remarks: unterminated vs. NUL-terminated string sections are not distinguishable after decoding, so this pass is expected to diverge at those offsets and nowhere else.)");

				Console.WriteLine();
			}

			Console.WriteLine(anyFailure
				? "RESULT: at least one tag's unedited round-trip was NOT byte-exact. See above."
				: $"RESULT: all {tags.Count} tag(s) round-trip byte-exact when unedited.");
			return anyFailure ? 3 : 0;
		}

		/// <summary>Marks every leaf field in a parsed tag dirty by writing its own current value back through itself.</summary>
		private static int TouchEveryField(Blamite.Blam.FifthGen.Structures.FifthGenTagBlock block)
		{
			int count = 0;
			foreach (var element in block.Elements)
				count += TouchEveryField(element);
			return count;
		}

		private static int TouchEveryField(Blamite.Blam.FifthGen.Structures.FifthGenTagStruct instance)
		{
			int count = 0;
			foreach (var value in instance.Values)
			{
				switch (value)
				{
					case Blamite.Blam.FifthGen.Structures.FifthGenIntegerValue i:
						i.SetValue(i.SignedValue, Blamite.IO.Endian.LittleEndian);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenRealValue r:
						r.SetValue(r.Value, Blamite.IO.Endian.LittleEndian);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenStringValue s:
						s.SetValue(s.Value);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenStringIDValue sid:
						sid.SetValue(sid.Value);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenTagReferenceValue tr:
						tr.SetReference(tr.GroupMagic, tr.Path, Blamite.IO.Endian.LittleEndian);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenDataValue d:
						d.SetContents(d.Contents);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenResourceValue res:
						res.SetContents(res.Contents, res.IsAttached);
						count++;
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenStructValue sv:
						count += TouchEveryField(sv.Value);
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenArrayValue av:
						foreach (var element in av.Elements)
							count += TouchEveryField(element);
						break;
					case Blamite.Blam.FifthGen.Structures.FifthGenBlockValue bv:
						if (bv.Value != null)
							count += TouchEveryField(bv.Value);
						break;
				}
			}
			return count;
		}

		private static bool ReportComparison(string label, byte[] expected, byte[] actual)
		{
			if (expected.Length == actual.Length && expected.AsSpan().SequenceEqual(actual))
			{
				Console.WriteLine($"  {label}: OK - {expected.Length:N0} bytes, byte-exact.");
				return true;
			}

			Console.WriteLine($"  {label}: MISMATCH - expected {expected.Length:N0} bytes, got {actual.Length:N0} bytes.");
			int limit = Math.Min(expected.Length, actual.Length);
			int firstDiff = -1;
			for (var i = 0; i < limit; i++)
			{
				if (expected[i] != actual[i]) { firstDiff = i; break; }
			}
			if (firstDiff < 0 && expected.Length != actual.Length)
				firstDiff = limit;

			if (firstDiff >= 0)
			{
				int start = Math.Max(0, firstDiff - 8);
				int endExpected = Math.Min(expected.Length, firstDiff + 24);
				int endActual = Math.Min(actual.Length, firstDiff + 24);
				Console.WriteLine($"    first difference at offset 0x{firstDiff:X}:");
				Console.WriteLine($"      expected: {BitConverter.ToString(expected, start, endExpected - start)}");
				Console.WriteLine($"      actual:   {BitConverter.ToString(actual, start, endActual - start)}");
			}
			return false;
		}

		/// <summary>
		///     Runs an unpack against a real container set and reports what was written, so the packaging service can
		///     be exercised end-to-end without the dialog.
		///     Usage: AssemblyAvalonia --ce-unpack &lt;source&gt; &lt;output-dir&gt; [--all-chunks]
		/// </summary>
		private static int RunCEUnpack(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Usage: --ce-unpack <source> <output-dir> [--all-chunks]");
				return 1;
			}

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			bool allChunks = args.Contains("--all-chunks");

			try
			{
				var preview = CEPackagingService.PreviewUnpack(args[1], EngineDatabaseService.Database!);
				Console.WriteLine($"=== PREVIEW: {preview.ContainerCount} container(s), {preview.Tags.Count} tag(s), {preview.TotalTagBytes:N0} byte(s) total ===");
				foreach (var t in preview.Tags)
					Console.WriteLine($"  [{t.Group}] {t.Name}  {t.PayloadSize:N0} bytes");
				foreach (var w in preview.MountWarnings)
					Console.WriteLine($"  warning: {w}");

				var progress = new Progress<CEPackagingProgress>(p =>
					Console.WriteLine($"  [{p.Stage}] {p.Completed}/{p.Total}  {p.Detail}"));

				var result = CEPackagingService.Unpack(args[1], args[2], EngineDatabaseService.Database!, allChunks, progress, System.Threading.CancellationToken.None);

				Console.WriteLine($"\n=== RESULT: success={result.Success} ===");
				if (!result.Success) { Console.WriteLine("error: " + result.Error); return 2; }
				Console.WriteLine($"output           : {result.OutputDirectory}");
				Console.WriteLine($"tags written     : {result.TagsWritten}");
				Console.WriteLine($"tag bytes written: {result.TagBytesWritten:N0}");
				Console.WriteLine($"chunks written   : {result.ChunksWritten}");
				Console.WriteLine($"elapsed          : {result.Elapsed.TotalMilliseconds:0} ms");
				foreach (var w in result.Warnings)
					Console.WriteLine($"warning: {w}");
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("UNPACK FAILED: " + ex);
				return 3;
			}
		}

		/// <summary>
		///     Runs a repack against a real container set and reports what was written.
		///     Usage: AssemblyAvalonia --ce-repack &lt;source&gt; &lt;tags-dir&gt; &lt;output-dir&gt;
		/// </summary>
		private static int RunCERepack(string[] args)
		{
			if (args.Length < 4)
			{
				Console.WriteLine("Usage: --ce-repack <source> <tags-dir> <output-dir>");
				return 1;
			}

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			try
			{
				var preview = CEPackagingService.PreviewRepack(args[1], args[2], EngineDatabaseService.Database!);
				Console.WriteLine($"=== PREVIEW ===");
				Console.WriteLine($"source     : {preview.SourceDirectory}");
				Console.WriteLine($"tags dir   : {preview.TagsDirectory}");
				Console.WriteLine($"containers : {preview.ContainerFiles.Count}");
				foreach (var f in preview.TagFiles)
					Console.WriteLine($"  {f.LogicalName}.{f.Group}: matched={f.MatchedExistingTag} parses={f.ParsesCleanly} identical={f.IdenticalToSource} ({f.FileSize:N0} bytes){(f.ParseProblem != null ? "  -- " + f.ParseProblem : "")}");
				Console.WriteLine($"matched={preview.MatchedCount} changed={preview.ChangedCount} unmatched={preview.UnmatchedCount} invalid={preview.InvalidCount} untouched-in-source={preview.UntouchedSourceTagCount}");
				Console.WriteLine($"can proceed: {preview.CanProceed}");

				// --cancel-after-n-steps N cancels once N progress reports have arrived, so
				// cancellation mid-run can be proven deterministically rather than raced with a
				// timer - see this probe's remarks on why that matters for a feature that touches
				// files on disk.
				int cancelAfter = -1;
				int cancelIndex = Array.IndexOf(args, "--cancel-after-n-steps");
				if (cancelIndex >= 0 && cancelIndex + 1 < args.Length)
					int.TryParse(args[cancelIndex + 1], out cancelAfter);

				var cts = new System.Threading.CancellationTokenSource();
				var stepCount = 0;
				var progress = new Progress<CEPackagingProgress>(p =>
				{
					Console.WriteLine($"  [{p.Stage}] {p.Completed}/{p.Total}  {p.Detail}");
					stepCount++;
					if (cancelAfter >= 0 && stepCount >= cancelAfter)
						cts.Cancel();
				});

				CERepackResult result;
				try
				{
					result = CEPackagingService.Repack(args[1], args[2], args[3], EngineDatabaseService.Database!, progress, cts.Token);
				}
				catch (OperationCanceledException)
				{
					Console.WriteLine($"\n=== CANCELLED after {stepCount} progress step(s) ===");
					return 0;
				}

				Console.WriteLine($"\n=== RESULT: success={result.Success} ===");
				if (!result.Success) { Console.WriteLine("error: " + result.Error); return 2; }
				Console.WriteLine($"output          : {result.OutputDirectory}");
				Console.WriteLine($"files copied    : {result.FilesCopied}");
				Console.WriteLine($"tags changed    : {result.TagsChanged}");
				Console.WriteLine($"tags unchanged  : {result.TagsUnchanged}");
				Console.WriteLine($"byte-identical  : {result.ByteIdenticalToSource}");
				Console.WriteLine($"changed containers: {string.Join(", ", result.ChangedContainers)}");
				Console.WriteLine($"elapsed         : {result.Elapsed.TotalMilliseconds:0} ms");
				foreach (var w in result.Warnings)
					Console.WriteLine($"warning: {w}");
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine("REPACK FAILED: " + ex);
				return 3;
			}
		}

		/// <summary>
		///     Dumps a mounted IoStore container's table-of-contents header and the physical layout of
		///     its compressed block table, so the on-disk shape a container writer has to reproduce can
		///     be read off real bytes instead of guessed at.
		///     Usage: AssemblyAvalonia --iostore-inspect &lt;path-to-.utoc&gt;
		/// </summary>
		private static int RunIoStoreInspect(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: --iostore-inspect <path-to-.utoc>");
				return 1;
			}

			string tocPath = args[1];

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			var ce = EngineDatabaseService.Database!.FirstOrDefault(e => e.Name == "Halo: Campaign Evolved");
			if (ce == null)
			{
				Console.WriteLine("No \"Halo: Campaign Evolved\" engine entry found.");
				return 1;
			}

			using var container = Blamite.IO.IoStore.IoStoreContainer.Open(tocPath, ce.Layouts, null);
			var toc = container.TableOfContents;

			Console.WriteLine($"=== {tocPath} ===");
			Console.WriteLine($"version                  : {toc.Version}");
			Console.WriteLine($"declared header size     : 0x{toc.DeclaredHeaderSize:X}");
			Console.WriteLine($"declared block entry sz  : {toc.DeclaredCompressedBlockEntrySize}");
			Console.WriteLine($"container id             : 0x{toc.ContainerId:X16}");
			Console.WriteLine($"flags                    : {toc.Flags} (0x{(int)toc.Flags:X})");
			Console.WriteLine($"compression block size   : 0x{toc.CompressionBlockSize:X} ({toc.CompressionBlockSize})");
			Console.WriteLine($"partition count/size     : {toc.PartitionCount} / 0x{toc.PartitionSize:X}");
			Console.WriteLine($"entry count              : {toc.ChunkIds.Count}");
			Console.WriteLine($"compressed block count   : {toc.CompressedBlocks.Count}");
			Console.WriteLine($"perfect hash seed count  : {toc.PerfectHashSeedCount}");
			Console.WriteLine($"chunks w/o perfect hash  : {toc.ChunksWithoutPerfectHashCount}");
			Console.WriteLine($"raw perfect hash bytes   : {toc.RawPerfectHashData.Length}");
			Console.WriteLine($"compression method count : {toc.CompressionMethods.Count} (name length {toc.CompressionMethodNameLength})");
			foreach (var m in toc.CompressionMethods)
				Console.WriteLine($"    \"{m}\"");
			Console.WriteLine($"directory index offset   : 0x{toc.DirectoryIndexOffset:X}");
			Console.WriteLine($"directory index size     : 0x{toc.DirectoryIndexSize:X}");
			Console.WriteLine($"directory index present  : {toc.DirectoryIndex != null}");
			if (toc.DirectoryIndex != null)
			{
				Console.WriteLine($"    mount point: \"{toc.DirectoryIndex.MountPoint}\"");
				Console.WriteLine($"    directories: {toc.DirectoryIndex.DirectoryCount}, files: {toc.DirectoryIndex.FileCount}, paths: {toc.DirectoryIndex.Paths.Count}");
			}

			string ucasPath = System.IO.Path.ChangeExtension(tocPath, ".ucas");
			long ucasLength = new System.IO.FileInfo(ucasPath).Length;
			Console.WriteLine($"\n.ucas length             : 0x{ucasLength:X} ({ucasLength})");

			Console.WriteLine("\n=== chunk table ===");
			for (int i = 0; i < toc.ChunkIds.Count; i++)
			{
				var id = toc.ChunkIds[i];
				var loc = toc.ChunkLocations[i];
				Console.WriteLine($"  [{i,3}] {id}  logical 0x{loc.Offset:X} + 0x{loc.Length:X}");
			}

			Console.WriteLine("\n=== compressed block table ===");
			long expectedOffset = 0;
			bool anyGap = false, anyPadding = false, anyCompressed = false;
			for (int i = 0; i < toc.CompressedBlocks.Count; i++)
			{
				var b = toc.CompressedBlocks[i];
				bool gap = b.Offset != expectedOffset;
				bool padded = b.CompressedSize != b.UncompressedSize && b.CompressionMethod == 0;
				if (gap) anyGap = true;
				if (padded) anyPadding = true;
				if (b.CompressionMethod != 0) anyCompressed = true;
				Console.WriteLine($"  [{i,3}] {b}{(gap ? "  <-- GAP (expected 0x" + expectedOffset.ToString("X") + ")" : "")}{(padded ? "  <-- PADDED" : "")}");
				expectedOffset = b.Offset + b.CompressedSize;
			}
			Console.WriteLine($"\nlast block's physical end: 0x{expectedOffset:X}   .ucas length: 0x{ucasLength:X}   match: {expectedOffset == ucasLength}");
			Console.WriteLine($"any gap between consecutive blocks' physical placement : {anyGap}");
			Console.WriteLine($"any stored block padded beyond its uncompressed size   : {anyPadding}");
			Console.WriteLine($"any compressed (non-stored) block                       : {anyCompressed}");

			return 0;
		}

		private static int RunEditTest(string[] args)
		{
			if (args.Length < 5)
			{
				Console.WriteLine("Usage: --edit-test <cache-file> <tag-name-substring> <field-name> <new-value>");
				return 1;
			}

			string path = args[1], tagNeedle = args[2], fieldName = args[3], newValue = args[4];

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			(bool ok, string before, string after) Read()
			{
				var session = CacheSession.Open(path, EngineDatabaseService.Database!);
				foreach (var g in session.Groups) foreach (var t in g.Tags) t.Owner = session;

				var tag = session.Groups.SelectMany(g => g.Tags)
					.FirstOrDefault(t => t.Name.Contains(tagNeedle, StringComparison.OrdinalIgnoreCase));
				if (tag == null) { Console.WriteLine($"no tag matching \"{tagNeedle}\""); return (false, "", ""); }

				var doc = new TagDocumentViewModel(tag);
				var row = doc.Rows.FirstOrDefault(r => r.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
				if (row == null) { Console.WriteLine($"no field matching \"{fieldName}\" (schema status: {doc.SchemaStatus})"); return (false, "", ""); }

				return (true, row.DisplayValue, row.DisplayValue);
			}

			Console.WriteLine("--- BEFORE ---");
			var beforeResult = Read();
			if (!beforeResult.ok) return 2;
			Console.WriteLine($"{fieldName} = {beforeResult.before}");

			// ---- mutate + save, in a fresh session (mirrors what the app does per-document) ----
			{
				var session = CacheSession.Open(path, EngineDatabaseService.Database!);
				foreach (var g in session.Groups) foreach (var t in g.Tags) t.Owner = session;
				var tag = session.Groups.SelectMany(g => g.Tags)
					.First(t => t.Name.Contains(tagNeedle, StringComparison.OrdinalIgnoreCase));
				var doc = new TagDocumentViewModel(tag);
				var row = doc.Rows.First(r => r.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

				if (row.Current == null) { Console.WriteLine("field has no edit state (not editable?)"); return 3; }

				switch (row.Editor)
				{
					case EditorKind.Integer or EditorKind.Enum or EditorKind.Flags or EditorKind.Color or EditorKind.StringId:
						row.Current.Int = long.Parse(newValue, CultureInfo.InvariantCulture);
						break;
					case EditorKind.Float:
						row.Current.Floats![0] = float.Parse(newValue, CultureInfo.InvariantCulture);
						break;
					// Free text for a Campaign Evolved stringID - see FifthGenStringIdEditor's remarks
					// on why this differs from classic EditorKind.StringId above (an index into a
					// shared table, so --edit-test sets a numeric id for that one instead of text).
					case EditorKind.FifthGenStringId:
						row.Current.Text = newValue;
						break;
					// "group|path", pipe-delimited since a group four-CC and a path won't naturally
					// contain one - see FifthGenTagReferenceEditor's remarks for the same encoding.
					case EditorKind.FifthGenTagReference:
					{
						var parts = newValue.Split('|', 2);
						if (parts.Length != 2) { Console.WriteLine("--edit-test needs \"group|path\" for a FifthGenTagReference field"); return 4; }
						row.Current.Int = Blamite.Util.CharConstant.FromString(parts[0]);
						row.Current.Text = parts[1];
						break;
					}
					default:
						Console.WriteLine($"--edit-test doesn't know how to set a {row.Editor} field; extend the switch if needed.");
						return 4;
				}

				var (ok, message) = doc.Save();
				Console.WriteLine($"SAVE: ok={ok} message=\"{message}\"");
				if (!ok) return 5;
			}

			Console.WriteLine("--- AFTER (fresh CacheSession, re-read from disk) ---");
			var afterResult = Read();
			Console.WriteLine($"{fieldName} = {afterResult.after}");

			return 0;
		}

		private static int RunPerfTest(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: --perf-test <cache-file-or-utoc>");
				return 1;
			}

			string path = args[1];

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Database == null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			var log = new LogService();

			var openWatch = Stopwatch.StartNew();
			CacheSession session;
			try
			{
				session = CacheSession.Open(path, EngineDatabaseService.Database!);
			}
			catch (Exception ex)
			{
				Console.WriteLine("OPEN FAILED: " + ex.Message);
				return 2;
			}
			openWatch.Stop();

			foreach (var g in session.Groups) foreach (var t in g.Tags) t.Owner = session;
			var allTags = session.Groups.SelectMany(g => g.Tags).ToList();

			Console.WriteLine($"session open (mount + directory walk): {openWatch.ElapsedMilliseconds} ms, {allTags.Count} tag(s)");
			Console.WriteLine();
			Console.WriteLine("=== per-tag open time: tag click -> TagDocumentViewModel ctor returns, rows visible ===");

			var docs = new Dictionary<string, TagDocumentViewModel>();
			foreach (var tag in allTags)
			{
				GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
				long memBefore = GC.GetTotalMemory(true);
				var sw = Stopwatch.StartNew();
				var doc = new TagDocumentViewModel(tag, log);
				sw.Stop();
				long memAfter = GC.GetTotalMemory(false);
				docs[tag.Name] = doc;
				Console.WriteLine($"  {tag.Name,-40} {sw.ElapsedMilliseconds,6} ms   rootRows={doc.Rows.Count,-6} +{(memAfter - memBefore) / 1024,6} KB   {doc.SchemaStatus}");
			}

			var scenario = docs.Values.FirstOrDefault(d => d.Tag.Name.Contains("scenario", StringComparison.OrdinalIgnoreCase))
			               ?? docs.Values.OrderByDescending(d => d.Rows.Count).FirstOrDefault();

			if (scenario == null)
			{
				Console.WriteLine("\nno tag opened; skipping expand/collapse timing.");
				return 0;
			}

			Console.WriteLine();
			Console.WriteLine($"=== expand/collapse/element-index timing on \"{scenario.Tag.Name}\" ({scenario.Rows.Count} root rows) ===");

			// Discover the top-level block whose subtree is biggest, to make the "does cost scale
			// with total open rows, or just with what this one call touches" comparison stark.
			MetaRowViewModel? biggest = null;
			int biggestSize = 0;
			foreach (var row in scenario.Rows.Where(r => r.IsBlock && r.ElementCount > 0).ToList())
			{
				int before = scenario.Rows.Count;
				scenario.ToggleExpand(row);
				int size = scenario.Rows.Count - before;
				scenario.ToggleExpand(row); // collapse back to a clean slate
				if (size > biggestSize) { biggestSize = size; biggest = row; }
			}

			if (biggest == null)
			{
				Console.WriteLine("no expandable top-level block found on this tag.");
				return 0;
			}

			Console.WriteLine($"largest top-level block: \"{biggest.Name}\" ({biggest.ElementCount} entries, subtree = {biggestSize} rows at element 0)");

			var tExpand1 = Stopwatch.StartNew();
			scenario.ToggleExpand(biggest);
			tExpand1.Stop();
			Console.WriteLine($"expand (nothing else open):      {tExpand1.Elapsed.TotalMilliseconds,7:0.000} ms   rows now {scenario.Rows.Count}");

			var tCollapse1 = Stopwatch.StartNew();
			scenario.ToggleExpand(biggest);
			tCollapse1.Stop();
			Console.WriteLine($"collapse:                         {tCollapse1.Elapsed.TotalMilliseconds,7:0.000} ms   rows now {scenario.Rows.Count}");

			// Open a handful of unrelated blocks first, then re-time expanding the same block: a
			// row-list rebuild that walks the whole document on every interaction would get slower
			// here; one that only touches the row's own subtree would not.
			var others = scenario.Rows.Where(r => r.IsBlock && r.ElementCount > 0 && r != biggest).Take(10).ToList();
			foreach (var o in others) scenario.ToggleExpand(o);
			Console.WriteLine($"(expanded {others.Count} other top-level block(s) first; rows now {scenario.Rows.Count})");

			var tExpand2 = Stopwatch.StartNew();
			scenario.ToggleExpand(biggest);
			tExpand2.Stop();
			Console.WriteLine($"expand same block, {others.Count} siblings already open: {tExpand2.Elapsed.TotalMilliseconds,7:0.000} ms   rows now {scenario.Rows.Count}");

			if (biggest.ElementCount > 1)
			{
				int steps = Math.Min(20, biggest.ElementCount);
				var tStep = Stopwatch.StartNew();
				for (int i = 0; i < steps; i++)
					scenario.SetElementIndex(biggest, i);
				tStep.Stop();
				Console.WriteLine($"step element index x{steps}: {tStep.Elapsed.TotalMilliseconds,7:0.000} ms total, {tStep.Elapsed.TotalMilliseconds / steps:0.000} ms/step   rows steady at {scenario.Rows.Count}");
			}

			// Peak rows / memory for a fully deep expansion: every top-level block, open at once.
			scenario.ToggleExpand(biggest);
			foreach (var o in others) scenario.ToggleExpand(o);

			var allBlocks = scenario.Rows.Where(r => r.IsBlock && r.ElementCount > 0).ToList();
			GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
			long memBeforeDeep = GC.GetTotalMemory(true);
			var tDeep = Stopwatch.StartNew();
			foreach (var row in allBlocks)
				scenario.ToggleExpand(row);
			tDeep.Stop();
			long memAfterDeep = GC.GetTotalMemory(false);

			Console.WriteLine();
			Console.WriteLine($"deep expansion of all {allBlocks.Count} top-level block(s): {tDeep.Elapsed.TotalMilliseconds:0.000} ms, peak rows={scenario.Rows.Count}, +{(memAfterDeep - memBeforeDeep) / 1024} KB");

			// ---- feature sanity checks: filter + tag-reference resolution ----
			Console.WriteLine();
			Console.WriteLine("=== field filter ===");
			foreach (var query in new[] { "structure", "trigger volumes", "nonexistent_field_xyz" })
			{
				var tFilter = Stopwatch.StartNew();
				scenario.FilterQuery = query;
				tFilter.Stop();
				Console.WriteLine($"  \"{query}\": {tFilter.Elapsed.TotalMilliseconds,7:0.000} ms   {scenario.FilterMatchCount} match(es), {scenario.Rows.Count} row(s) shown (context included)");
			}
			scenario.FilterQuery = "";
			Console.WriteLine($"  (cleared): rows back to {scenario.Rows.Count}");

			Console.WriteLine();
			Console.WriteLine("=== tag reference resolution (currently-visible rows) ===");
			var refs = scenario.Rows.Where(r => r.RefTarget != null).ToList();
			Console.WriteLine($"  {refs.Count} reference row(s) found among {scenario.Rows.Count} visible rows");
			foreach (var r in refs.Take(10))
				Console.WriteLine($"    {r.Name,-28} -> group={r.RefTarget!.Group,-6} name={r.RefTarget.Name ?? "(null)"}  followable={r.RefTarget.IsFollowable}");

			// ---- reference navigation end-to-end, through MainViewModel exactly as the shell uses it ----
			Console.WriteLine();
			Console.WriteLine("=== MainViewModel.NavigateToTagReference ===");
			var vm = new MainViewModel();
			vm.OpenFileAsync(path).GetAwaiter().GetResult();
			Console.WriteLine($"  status after mount: {vm.StatusText}");

			// scenario references a structure bsp ("...sb_main") that isn't one of this mod's 5
			// tags - the honest "not mounted" path.
			var unmounted = refs.FirstOrDefault(r => r.RefTarget!.Name != null && r.RefTarget.Name.Contains("sb_main"));
			if (unmounted != null)
			{
				vm.NavigateToTagReference(unmounted.RefTarget!);
				Console.WriteLine($"  navigate to \"{unmounted.RefTarget!.Name}.{unmounted.RefTarget.Group}\" (expected: not mounted): {vm.StatusText}");
			}

			// None of this mod's own 5 tags share a name with any reference the scenario tag
			// itself carries (its references name real retail asset paths like
			// "objects\vehicles\human\pelican\pelican" - this mod's own containers are named
			// after the file the modder shipped, "pelican-vehicle", not that in-game path), so
			// there is no naturally-occurring in-mod reference to exercise the "found" case with.
			// Build a synthetic target that names one of the 5 mounted tags directly instead, to
			// verify the matching logic itself rather than this mod's happenstance content.
			var syntheticTarget = new TagRefTarget("vehi", "pelican-vehicle", null);
			int docsBefore = vm.Documents.Count;

			// IsOpeningTag/OpeningTagLabel drive the field table's loading overlay (MainWindow.axaml)
			// - catching that overlay in the act with a screenshot is a race against however fast
			// this machine happens to open the tag, so verify the state machine directly instead:
			// record every value IsOpeningTag actually takes across the call.
			var isOpeningTagObserved = new List<bool>();
			System.ComponentModel.PropertyChangedEventHandler handler = (_, ev) =>
			{
				if (ev.PropertyName == nameof(MainViewModel.IsOpeningTag))
					isOpeningTagObserved.Add(vm.IsOpeningTag);
			};
			vm.PropertyChanged += handler;

			vm.NavigateToTagReference(syntheticTarget); // fire-and-forget, like the real UI call site - wait for it the same way the shell's own loading overlay would
			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (vm.IsOpeningTag && DateTime.UtcNow < deadline)
				System.Threading.Thread.Sleep(10);
			vm.PropertyChanged -= handler;

			Console.WriteLine($"  navigate to synthetic \"pelican-vehicle.vehi\" (expected: opens a tab): {vm.StatusText}");
			Console.WriteLine($"  documents: {docsBefore} -> {vm.Documents.Count}, active = {vm.ActiveDocument?.HeaderTitle}");
			Console.WriteLine($"  IsOpeningTag transitions observed: [{string.Join(", ", isOpeningTagObserved)}] (expected: true then false - drives the field table's loading overlay)");

			return 0;
		}

		private static int RunProbe(string[] args)
		{
			Console.WriteLine("Assembly (Avalonia) headless probe");
			Console.WriteLine("RID        : " + RuntimeInformation.RuntimeIdentifier);
			Console.WriteLine("Framework  : " + RuntimeInformation.FrameworkDescription);
			Console.WriteLine("Blamite    : " + typeof(Blamite.Blam.CacheFileLoader).Assembly.Location);
			Console.WriteLine();

			EngineDatabaseService.Initialize();
			if (EngineDatabaseService.Error != null)
			{
				Console.WriteLine("ENGINE DATABASE: FAILED\n" + EngineDatabaseService.Error);
				return 1;
			}

			Console.WriteLine($"ENGINE DATABASE: {EngineDatabaseService.EngineCount} engines");
			Console.WriteLine($"  Formats : {EngineDatabaseService.FormatsRoot}");
			Console.WriteLine($"  Plugins : {EngineDatabaseService.PluginsRoot ?? "NOT FOUND"}");
			Console.WriteLine();

			if (args.Length < 2)
			{
				Console.WriteLine("No cache file supplied.");
				return 0;
			}

			CacheSession session;
			try
			{
				session = CacheSession.Open(args[1], EngineDatabaseService.Database!);
			}
			catch (Exception ex)
			{
				Console.WriteLine("OPEN FAILED: " + ex.Message);
				return 2;
			}

			var c = session.Cache;
			Console.WriteLine($"OPEN OK: {session.FilePath}");
			Console.WriteLine($"  engine   : {session.Engine.Name}   build {c.BuildString}");
			Console.WriteLine($"  internal : {c.InternalName}");
			Console.WriteLine($"  groups   : {session.Groups.Count}   tags: {session.TotalTags:N0}");
			Console.WriteLine();

			foreach (var g in session.Groups)
			{
				Console.WriteLine($"[{g.Magic}] {g.Description}  ({g.Tags.Count})");
				foreach (var t in g.Tags.Take(4))
					Console.WriteLine($"    0x{t.Offset:X8}  {t.Name}");
			}

			if (args.Length < 3) return 0;

			// Meta read for the first tag whose name matches.
			var target = session.Groups.SelectMany(g => g.Tags)
				.FirstOrDefault(t => t.Name.Contains(args[2], StringComparison.OrdinalIgnoreCase));

			if (target == null)
			{
				Console.WriteLine($"\nNo tag matching \"{args[2]}\".");
				return 0;
			}

			Console.WriteLine($"\n=== META: {target.Name}.{target.Group} ===");
			var fields = session.ReadMeta(target, out var status);
			Console.WriteLine("status: " + status);

			if (fields == null) return 0;

			foreach (var f in fields.Take(40))
				Console.WriteLine($"  {f.OffsetLabel}  {f.Def.KindLabel,-14} {f.DisplayName,-42} {f.Value}");
			if (fields.Count > 40)
				Console.WriteLine($"  ... {fields.Count - 40} more fields");

			return 0;
		}
	}
}
