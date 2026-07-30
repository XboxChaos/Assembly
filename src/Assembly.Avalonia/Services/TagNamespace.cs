using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Blamite.Serialization;

namespace Assembly.Avalonia.Services
{
	public enum SourceKind { File, Folder, Zip }

	/// <summary>
	///     One mounted container: a single cache file, a folder scanned for cache files, or a
	///     zip extracted and scanned the same way. Holds every <see cref="CacheSession" /> it
	///     contributed (a folder or zip commonly contributes many).
	/// </summary>
	public sealed class MountedSource
	{
		public MountedSource(SourceKind kind, string path)
		{
			Kind = kind;
			Path = path;
			DisplayName = System.IO.Path.GetFileName(path.TrimEnd('/', '\\'));
			if (string.IsNullOrEmpty(DisplayName)) DisplayName = path;
		}

		public SourceKind Kind { get; }
		public string Path { get; }
		public string DisplayName { get; }
		public List<CacheSession> Sessions { get; } = new();

		/// <summary>Files that looked like candidates but failed to open, with the reason why.</summary>
		public List<(string File, string Reason)> Failures { get; } = new();

		/// <summary>Only set for Zip mounts: the temp directory entries were extracted to, cleaned up on unmount.</summary>
		public string? ExtractedTempDir { get; set; }

		public int TagCount => Sessions.Sum(s => s.TotalTags);

		public string SummaryLabel => Kind switch
		{
			SourceKind.File => $"{Sessions.Sum(s => s.TotalTags):N0} tags",
			SourceKind.Folder => $"{Sessions.Count} cache file{(Sessions.Count == 1 ? "" : "s")}, {TagCount:N0} tags",
			SourceKind.Zip => $"{Sessions.Count} cache file{(Sessions.Count == 1 ? "" : "s")}, {TagCount:N0} tags",
			_ => ""
		};
	}

	/// <summary>
	///     A mounted tag namespace: zero or more <see cref="MountedSource" />s whose tags are
	///     aggregated into one flat namespace, grouped by tag group or folder path exactly as
	///     Assembly's tag browser does for a single map. This is deliberately not "one open
	///     file" — Campaign Evolved mounts are N containers (a mod is six separate _P sets, one
	///     tag each; the game's Paks folder has 28), and the scenario tag is one tag among
	///     ~12,000 in a flat namespace rather than a natural tree root. Opening a folder or a
	///     zip full of classic .map files exercises the same "many containers, one namespace"
	///     shape for real. Blamite now parses CE's UE5 IoStore containers too, so a folder of
	///     .utoc files mounts natively as well.
	/// </summary>
	public sealed class TagNamespace : IDisposable
	{
		private static readonly string[] CacheExtensions = { ".map", ".yelo", ".campaign", ".utoc" };

		/// <summary>
		///     Extensions whose loader mounts every sibling container in the same directory by
		///     itself, so only one of them should ever be opened per directory.
		/// </summary>
		private static readonly string[] SelfMountingExtensions = { ".utoc" };

		public ObservableCollection<MountedSource> Sources { get; } = new();

		public IEnumerable<CacheSession> Sessions => Sources.SelectMany(s => s.Sessions);
		public IEnumerable<TagInfo> AllTags => Sessions.SelectMany(s => s.Groups).SelectMany(g => g.Tags);
		public int TotalTags => Sessions.Sum(s => s.TotalTags);
		public bool HasAnySource => Sources.Count > 0;
		public bool HasMultipleSources => Sources.Count > 1 || Sessions.Count() > 1;

		public event Action<string>? Log;

		private void Emit(string message) => Log?.Invoke(message);

		/// <summary>Mounts a single cache file.</summary>
		public MountedSource MountFile(string path, EngineDatabase db)
		{
			var source = new MountedSource(SourceKind.File, path);
			TryOpenInto(source, path, db);
			Sources.Add(source);
			Emit(source.Sessions.Count > 0
				? $"mounted \"{source.DisplayName}\" ({source.SummaryLabel})"
				: $"failed to mount \"{source.DisplayName}\": {source.Failures.FirstOrDefault().Reason}");
			return source;
		}

		/// <summary>
		///     Mounts every recognized cache file found (recursively) under a folder. This is
		///     the real mechanism a Campaign Evolved mount needs (N containers, not one file);
		///     it is exercised end-to-end against classic .map fixtures because Blamite has no
		///     CE IoStore reader to point it at yet.
		/// </summary>
		public MountedSource MountFolder(string path, EngineDatabase db)
		{
			var source = new MountedSource(SourceKind.Folder, path);
			IEnumerable<string> candidates;
			try
			{
				candidates = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
					.Where(f => CacheExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
					.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
			}
			catch (Exception ex)
			{
				source.Failures.Add((path, ex.Message));
				Sources.Add(source);
				Emit($"failed to scan folder \"{path}\": {ex.Message}");
				return source;
			}

			// A Campaign Evolved cache file already *is* a mount: handed any one .utoc,
			// FifthGenCacheFile mounts every container sitting beside it and resolves overrides
			// across the lot. Opening all six of a mod's containers individually would therefore
			// produce six sessions showing the same five tags. Keep only the first self-mounting
			// container per directory and let the loader expand it.
			candidates = candidates
				.GroupBy(f => System.IO.Path.GetDirectoryName(f) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.SelectMany(dir => dir
					.GroupBy(f => SelfMountingExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
					.SelectMany(kind => kind.Key ? kind.Take(1) : kind))
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

			int found = 0;
			foreach (var file in candidates)
			{
				found++;
				TryOpenInto(source, file, db);
			}

			Sources.Add(source);
			Emit(found == 0
				? $"mounted folder \"{source.DisplayName}\": no recognized cache files found (looked for {string.Join(", ", CacheExtensions)})"
				: $"mounted folder \"{source.DisplayName}\": {source.SummaryLabel}" +
				  (source.Failures.Count > 0 ? $", {source.Failures.Count} failed" : ""));
			return source;
		}

		/// <summary>
		///     Extracts a zip's recognized cache files to a temp directory and mounts each one,
		///     the same way a folder mount does. Mirrors opening one of the six per-container
		///     zips a Campaign Evolved mod ships as.
		/// </summary>
		public MountedSource MountZip(string path, EngineDatabase db)
		{
			var source = new MountedSource(SourceKind.Zip, path);
			string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AssemblyAvalonia-mount-" + Guid.NewGuid().ToString("N"));

			try
			{
				Directory.CreateDirectory(tempDir);
				using var zip = ZipFile.OpenRead(path);
				var entries = zip.Entries
					.Where(e => CacheExtensions.Contains(System.IO.Path.GetExtension(e.FullName).ToLowerInvariant()))
					.ToList();

				if (entries.Count == 0)
				{
					Emit($"mounted zip \"{source.DisplayName}\": no recognized cache files inside " +
					     $"(looked for {string.Join(", ", CacheExtensions)}; {zip.Entries.Count} entries total)");
				}

				foreach (var entry in entries)
				{
					var destName = System.IO.Path.GetFileName(entry.FullName);
					var dest = System.IO.Path.Combine(tempDir, destName);
					// Guard against duplicate leaf names / zip-slip from a hostile archive.
					dest = UniquePath(dest);
					if (!System.IO.Path.GetFullPath(dest).StartsWith(System.IO.Path.GetFullPath(tempDir), StringComparison.Ordinal))
						continue;

					entry.ExtractToFile(dest, overwrite: true);
					TryOpenInto(source, dest, db);
				}

				source.ExtractedTempDir = tempDir;
			}
			catch (Exception ex)
			{
				source.Failures.Add((path, ex.Message));
				Emit($"failed to mount zip \"{path}\": {ex.Message}");
			}

			Sources.Add(source);
			if (source.Sessions.Count > 0)
				Emit($"mounted zip \"{source.DisplayName}\": {source.SummaryLabel}" +
				     (source.Failures.Count > 0 ? $", {source.Failures.Count} failed" : ""));
			return source;
		}

		public void Unmount(MountedSource source)
		{
			Sources.Remove(source);
			foreach (var s in source.Sessions) s.Dispose();
			if (source.ExtractedTempDir != null)
			{
				try { Directory.Delete(source.ExtractedTempDir, recursive: true); }
				catch { /* best effort */ }
			}
			Emit($"unmounted \"{source.DisplayName}\"");
		}

		public void UnmountAll()
		{
			foreach (var s in Sources.ToList()) Unmount(s);
		}

		private void TryOpenInto(MountedSource source, string file, EngineDatabase db)
		{
			try
			{
				var session = CacheSession.Open(file, db);
				foreach (var g in session.Groups)
					foreach (var t in g.Tags)
					{
						t.Owner = session;
						t.SourceName = System.IO.Path.GetFileName(file);
					}
				source.Sessions.Add(session);
			}
			catch (Exception ex)
			{
				source.Failures.Add((System.IO.Path.GetFileName(file), ex.Message));
			}
		}

		private static string UniquePath(string path)
		{
			if (!File.Exists(path)) return path;
			var dir = System.IO.Path.GetDirectoryName(path)!;
			var name = System.IO.Path.GetFileNameWithoutExtension(path);
			var ext = System.IO.Path.GetExtension(path);
			int i = 1;
			string candidate;
			do { candidate = System.IO.Path.Combine(dir, $"{name}_{i++}{ext}"); }
			while (File.Exists(candidate));
			return candidate;
		}

		public void Dispose() => UnmountAll();
	}
}
