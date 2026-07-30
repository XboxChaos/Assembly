using System;
using System.Collections.Generic;

namespace Assembly.Avalonia.Services
{
	/// <summary>
	///     One searchable entry in the command palette's tag-jump (plain-text) mode. Deliberately
	///     independent of <see cref="TagInfo" />/<c>ITag</c> - it carries only what fuzzy search
	///     and the result row need (name/group/source plus a precomputed lower-case name), with an
	///     opaque <see cref="Token" /> the caller hands back to open the real thing. That
	///     decoupling is what lets <c>--palette-bench</c> (<c>HeadlessProbe</c>) synthesize a
	///     12,000-entry corpus for latency measurement without needing a real mounted namespace or
	///     touching <c>TagNamespace</c>/<c>CacheSession</c> (owned by the packaging pass this wave).
	/// </summary>
	public readonly struct TagSearchEntry
	{
		public TagSearchEntry(string name, string group, string sourceName, object? token)
		{
			Name = name;
			Group = group;
			SourceName = sourceName;
			LowerName = name.ToLowerInvariant();
			LowerGroup = group.ToLowerInvariant();
			Token = token;
		}

		public string Name { get; }
		public string Group { get; }
		public string SourceName { get; }
		public string LowerName { get; }
		public string LowerGroup { get; }

		/// <summary>Opaque handle the caller supplied - a <c>TagInfo</c> in production.</summary>
		public object? Token { get; }
	}

	/// <summary>
	///     A flat, rebuild-on-mount-change index over every tag in the namespace. Built once per
	///     <see cref="Rebuild" /> call (the command palette calls it only when the mounted
	///     namespace's <c>TagsVersion</c> actually changes - see <c>MainViewModel</c>), not on
	///     every keystroke; <see cref="Search" /> itself allocates no view models, only a bounded
	///     list of scored struct entries, so a 12,000-tag namespace costs one array build per mount
	///     and a cheap scan per keystroke rather than 12,000 object allocations per keystroke.
	/// </summary>
	public sealed class TagSearchIndex
	{
		private TagSearchEntry[] _entries = Array.Empty<TagSearchEntry>();

		// Reused across calls to Search (see its own remarks) for the "group name" fallback scan -
		// an instance field rather than a per-call local so a long tag name only ever grows this
		// once, on the first keystroke that needs a buffer that big, rather than once per keystroke.
		private char[] _scratch = Array.Empty<char>();

		public int Count => _entries.Length;

		public void Rebuild(IReadOnlyList<TagSearchEntry> entries)
		{
			var array = new TagSearchEntry[entries.Count];
			for (int i = 0; i < entries.Count; i++) array[i] = entries[i];
			_entries = array;
		}

		/// <summary>
		///     Scores every entry against <paramref name="query" /> and returns the best
		///     <paramref name="max" /> as (entry, score) pairs, highest score first. An empty query
		///     matches nothing - with up to 12,000 entries and nothing typed yet there is no
		///     meaningful "top" result, so the caller (see <c>CommandPalette</c>) shows recently
		///     -opened tags instead of an arbitrary namespace slice.
		/// </summary>
		public List<(TagSearchEntry Entry, int Score)> Search(string query, int max)
		{
			var results = new List<(TagSearchEntry, int)>(max);
			query = query.Trim();
			if (query.Length == 0 || _entries.Length == 0) return results;

			string lowerQuery = query.ToLowerInvariant();
			ReadOnlySpan<char> querySpan = lowerQuery;

			foreach (var entry in _entries)
			{
				var score = FuzzyMatch.Score(entry.LowerName, querySpan);
				if (score == null)
				{
					// A tag name rarely contains the group magic, but "weap pelican" (group-
					// qualified search) is a real thing someone might type - fall back to scoring
					// "group name" as one span (built in _scratch, not a fresh heap string per
					// candidate - see that field's own remarks) so that still matches, at a flat
					// discount so a pure name match is never outranked by a weaker group-assisted one.
					int needed = entry.LowerGroup.Length + 1 + entry.LowerName.Length;
					if (_scratch.Length < needed) _scratch = new char[needed];
					entry.LowerGroup.AsSpan().CopyTo(_scratch);
					_scratch[entry.LowerGroup.Length] = ' ';
					entry.LowerName.AsSpan().CopyTo(_scratch.AsSpan(entry.LowerGroup.Length + 1));

					var groupScore = FuzzyMatch.Score(_scratch.AsSpan(0, needed), querySpan);
					if (groupScore == null) continue;
					score = groupScore - 30;
				}

				InsertRanked(results, (entry, score.Value), max);
			}

			return results;
		}

		/// <summary>Keeps <paramref name="results" /> sorted descending by score with at most
		/// <paramref name="max" /> entries, without re-sorting the whole list on every insert - for
		/// a namespace this size the match count can run into the thousands for a one-character
		/// query, and only the top ~30-50 are ever shown.</summary>
		private static void InsertRanked(List<(TagSearchEntry Entry, int Score)> results,
			(TagSearchEntry Entry, int Score) candidate, int max)
		{
			if (results.Count == max && candidate.Score <= results[^1].Score) return;

			int i = results.Count - 1;
			results.Add(candidate); // placeholder to grow the list, shifted into place below
			while (i >= 0 && results[i].Score < candidate.Score)
			{
				results[i + 1] = results[i];
				i--;
			}
			results[i + 1] = candidate;

			if (results.Count > max) results.RemoveAt(results.Count - 1);
		}
	}
}
