using System;
using System.Linq;
using System.Runtime.InteropServices;
using Assembly.Avalonia.Services;

namespace Assembly.Avalonia
{
	/// <summary>
	///     Non-GUI entry point that exercises the same services the window uses, so the
	///     Blamite integration can be verified from a terminal.
	///     Usage: AssemblyAvalonia --headless [cache-file] [tag-name-substring]
	/// </summary>
	internal static class HeadlessProbe
	{
		public static int Run(string[] args)
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
