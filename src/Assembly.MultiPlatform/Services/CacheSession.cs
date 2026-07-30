using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Blamite.Blam;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Serialization;
using Blamite.Util;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     An open cache file plus everything needed to keep reading from it: the engine
	///     description, a stream manager for on-demand meta reads, and a plugin cache.
	/// </summary>
	public sealed class CacheSession : IDisposable
	{
		/// <summary>
		///     Shown in the tag tree's description column for a group neither the cache's own StringID table nor the
		///     engine's <c>groupNames</c> database can name.
		/// </summary>
		/// <remarks>
		///     Deliberately not "unknown": that word reads as an error, as if the lookup itself had failed, when what
		///     is actually true is narrower - nobody has told this codebase what to call the group yet. This is the
		///     honest ceiling for a fifth-generation (Campaign Evolved) group this codebase has not yet catalogued in
		///     <c>Formats/CampaignEvolved/CE_GroupNames.xml</c> (see that file's own remarks): a CE tag payload states
		///     only its own four-CC, never an English name, and there is no per-cache StringID table to fall back to
		///     either (<see cref="Blamite.Blam.FifthGen.FifthGenCacheFile.StringIDs" /> is always <c>null</c>), so a
		///     name for a group this file has not seen yet can only come from someone adding it once real data
		///     confirms what it should say.
		/// </remarks>
		private const string NoGroupNameLabel = "(unnamed group)";

		private readonly Dictionary<string, IReadOnlyList<MetaFieldDef>> _pluginCache = new();

		private CacheSession(string path, ICacheFile cache, EngineDescription engine, FileStreamManager streams)
		{
			FilePath = path;
			Cache = cache;
			Engine = engine;
			Streams = streams;
		}

		public string FilePath { get; }
		public ICacheFile Cache { get; }
		public EngineDescription Engine { get; }
		public FileStreamManager Streams { get; }

		public int AmbiguousMatches { get; private init; }
		public List<TagGroupInfo> Groups { get; private init; } = new();
		public int TotalTags { get; private init; }
		public int SkippedTags { get; private init; }

		public string DisplayName => Path.GetFileName(FilePath);

		/// <summary>Opens a cache file, or throws with a message suitable for display.</summary>
		public static CacheSession Open(string path, EngineDatabase db, Func<List<EngineDescription>, EngineDescription>? chooser = null)
		{
			using var stream = File.OpenRead(path);
			using var reader = new EndianReader(stream, Endian.BigEndian);

			List<EngineDescription> matches;
			try
			{
				matches = CacheFileLoader.FindEngineDescriptions(reader, db);
			}
			// EndOfStreamException belongs here alongside ArgumentException. Now that EndianReader
			// reads exactly rather than tolerating a short read, handing detection a file that is
			// too small, or simply not a cache - a .pak sitting beside a .utoc, say - fails by
			// running out of bytes rather than by mismatching a value. Both mean the same thing to
			// someone who just opened the wrong file, and both deserve the same sentence.
			catch (Exception ex) when (ex is ArgumentException || ex is EndOfStreamException)
			{
				throw new InvalidDataException($"This does not look like a Halo cache file.\n\nBlamite said: {ex.Message}");
			}

			if (matches.Count == 0)
				throw new InvalidDataException(
					"No engine definition matches this file's build.\n\n" +
					"Halo Infinite .module files are also reported this way; they are not supported.");

			var engine = matches.Count > 1 && chooser != null ? chooser(matches) : matches[0];

			var cache = CacheFileLoader.LoadCacheFileWithEngineDescription(reader, path, engine);

			var streamPath = engine.Engine == EngineType.Eldorado
				? ((Blamite.Blam.Eldorado.EldoradoCacheFile)cache).TagFilePath
				: path;
			var streams = new FileStreamManager(streamPath, cache.Endianness);

			var (groups, total, skipped) = Project(cache, engine);

			return new CacheSession(path, cache, engine, streams)
			{
				AmbiguousMatches = matches.Count,
				Groups = groups,
				TotalTags = total,
				SkippedTags = skipped
			};
		}

		private static (List<TagGroupInfo>, int, int) Project(ICacheFile cache, EngineDescription engine)
		{
			var byMagic = new Dictionary<int, TagGroupInfo>();

			foreach (ITagGroup g in cache.TagGroups)
			{
				if (g == null) continue;
				string magic = CharConstant.ToString(g.Magic);
				string desc = g.Description.Value == 0
					? engine.GroupNames?.RetrieveName(magic) ?? NoGroupNameLabel
					: cache.StringIDs?.GetString(g.Description) ?? NoGroupNameLabel;
				byMagic[g.Magic] = new TagGroupInfo(magic, desc);
			}

			int skipped = 0;
			foreach (ITag tag in cache.Tags)
			{
				if (tag?.Group == null) { skipped++; continue; }

				// A null MetaLocation only means "empty tag" for engines that address meta by a
				// pointer into the cache file. Second-generation caches legitimately leave it
				// null, and fifth-generation tags are whole files carried in their own IoStore
				// chunk, so they have no cache-relative location to point at at all.
				bool addressesMetaByPointer = cache.Engine != EngineType.SecondGeneration
				                              && cache.Engine != EngineType.FifthGeneration;
				if (addressesMetaByPointer && tag.MetaLocation == null) { skipped++; continue; }

				if (!byMagic.TryGetValue(tag.Group.Magic, out var gi))
				{
					gi = new TagGroupInfo(CharConstant.ToString(tag.Group.Magic), "<not in group table>");
					byMagic[tag.Group.Magic] = gi;
				}

				string name = cache.FileNames?.GetTagName(tag) ?? $"<unnamed {tag.Index}>";
				gi.Tags.Add(new TagInfo(tag, name, gi.Magic));
			}

			var groups = byMagic.Values.Where(g => g.Tags.Count > 0)
				.OrderBy(g => g.Magic, StringComparer.Ordinal).ToList();
			foreach (var g in groups)
				g.Tags.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

			return (groups, groups.Sum(g => g.Tags.Count), skipped);
		}

		/// <summary>
		///     Reads a tag's meta using its group's tag-definition XML. Returns null (with a
		///     reason) when no definition exists for the group, which is common and not an error.
		/// </summary>
		public List<MetaFieldValue>? ReadMeta(TagInfo tag, out string status)
		{
			if (tag.Raw.MetaLocation == null)
			{
				status = "This tag has no meta.";
				return null;
			}

			IReadOnlyList<MetaFieldDef> defs;
			try
			{
				defs = GetSchema(tag.Group, out var schemaStatus);
				if (defs.Count == 0) { status = schemaStatus; return null; }
			}
			catch (Exception ex)
			{
				status = $"Failed to parse tag definition: {ex.Message}";
				return null;
			}

			try
			{
				using var reader = Streams.OpenRead();
				long baseOffset = tag.Raw.MetaLocation.AsOffset();
				var values = MetaValueReader.Read(reader, baseOffset, defs, Cache);
				status = $"{values.Count} fields from {tag.Group}.xml";
				return values;
			}
			catch (Exception ex)
			{
				status = $"Failed to read meta: {ex.GetType().Name}: {ex.Message}";
				return null;
			}
		}

		/// <summary>Public entry point for consumers (e.g. TagDocumentViewModel) that need the raw
		/// schema plus their own reader, instead of the flat pre-formatted value list <see cref="ReadMeta"/> returns.</summary>
		public IReadOnlyList<MetaFieldDef> GetSchema(TagInfo tag, out string status) => GetSchema(tag.Group, out status);

		private IReadOnlyList<MetaFieldDef> GetSchema(string groupMagic, out string status)
		{
			if (_pluginCache.TryGetValue(groupMagic, out var cached))
			{
				status = cached.Count == 0 ? "No tag definition for this group." : "";
				return cached;
			}

			var path = ResolvePluginPath(groupMagic);
			if (path == null)
			{
				status = $"No tag definition (plugin) found for group '{groupMagic}'.";
				_pluginCache[groupMagic] = Array.Empty<MetaFieldDef>();
				return _pluginCache[groupMagic];
			}

			var visitor = new PluginSchemaVisitor();
			using (var xr = XmlReader.Create(path))
				AssemblyPluginLoader.LoadPlugin(xr, visitor);

			var list = visitor.Fields.ToList();
			_pluginCache[groupMagic] = list;
			status = list.Count == 0 ? "Tag definition is empty." : "";
			return list;
		}

		private string? ResolvePluginPath(string groupMagic)
		{
			var root = EngineDatabaseService.PluginsRoot;
			if (root == null) return null;

			var file = Sterilize(groupMagic).Trim() + ".xml";

			foreach (var key in new[] { "plugins", "fallbackPlugins" })
			{
				if (!Engine.Settings.PathExists(key)) continue;
				var dir = Engine.Settings.GetSetting<string>(key);
				if (string.IsNullOrEmpty(dir)) continue;
				var candidate = Path.Combine(root, dir, file);
				if (File.Exists(candidate)) return candidate;
			}

			return null;
		}

		/// <summary>Mirrors VariousFunctions.SterilizeTagGroupName in the WPF app.</summary>
		private static string Sterilize(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
			var s = new string(chars);
			return s.TrimEnd('.').Length == 0 ? s : s.TrimEnd('.');
		}

		public void Dispose() { }
	}

	public sealed class TagGroupInfo
	{
		public TagGroupInfo(string magic, string description)
		{
			Magic = magic;
			Description = description;
		}

		public string Magic { get; }
		public string Description { get; }
		public List<TagInfo> Tags { get; } = new();
	}

	public sealed class TagInfo
	{
		public TagInfo(ITag raw, string name, string group)
		{
			Raw = raw;
			Name = name;
			Group = group;
		}

		public ITag Raw { get; }
		public string Name { get; }
		public string Group { get; }

		/// <summary>
		///     The mounted cache this tag belongs to. A "cache" here is a mount of N containers
		///     (folder / zip / single file), not necessarily one file, so every tag carries a
		///     back-reference to the specific session that owns it rather than the tree assuming
		///     a single global session. Set once by <see cref="TagNamespace" /> right after the
		///     owning session opens.
		/// </summary>
		public CacheSession? Owner { get; set; }

		/// <summary>Display label for the mounted source this tag came from (e.g. a file name).</summary>
		public string SourceName { get; set; } = "";

		public uint Offset => Raw.MetaLocation?.AsOffset() ?? 0;
		public long Pointer => Raw.MetaLocation?.AsPointer() ?? 0;
		public string Index => Raw.Index.ToString();
	}
}
