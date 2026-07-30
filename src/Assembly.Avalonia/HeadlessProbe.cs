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
	///
	///     Also carries --ce-fields, which lists a Campaign Evolved tag's rows - top-level plus,
	///     optionally, every field under one expanded top-level block by name - with each row's
	///     Editor/Kind/DisplayValue/IsDirty, since session.ReadMeta (what --headless's own meta
	///     listing uses) is a classic plugin-XML path a fifth-generation tag never had any meta
	///     for in the first place. Meant for finding real field names to point --edit-test at,
	///     not as a proof of anything itself.
	///     Usage: AssemblyAvalonia --ce-fields utoc-or-folder tag-name-substring [block-to-expand]
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

			return RunProbe(args);
		}

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
