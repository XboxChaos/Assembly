using System;
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
	/// </summary>
	internal static class HeadlessProbe
	{
		public static int Run(string[] args)
		{
			if (args.Length > 0 && args[0] == "--edit-test")
				return RunEditTest(args);

			return RunProbe(args);
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
