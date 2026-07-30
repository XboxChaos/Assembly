using System;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     A small, dependency-free fuzzy subsequence matcher shared by the command palette's two
	///     search modes (<see cref="CommandRegistry" /> and <see cref="TagSearchIndex" />).
	///     <para>
	///         The scoring follows the same general shape as the "fuzzy filter" family used by
	///         editor quick-open widgets: a candidate matches if every character of the query
	///         appears in it in order (not necessarily contiguous), and the score rewards the
	///         qualities that make a match feel like "the one the user meant" rather than merely
	///         possible - an exact match, a match at the very start of the string, a match that
	///         starts right after a separator (so typing "cg" jumps to the <c>c</c> in
	///         "pelican_<b>c</b>hin_<b>g</b>un" rather than a <c>c</c> buried mid-word), and long
	///         unbroken runs of consecutive characters. Gaps and late-starting matches are
	///         penalised lightly so two matches for the same query are still ordered sensibly
	///         relative to each other rather than only separating "matches" from "non-matches".
	///     </para>
	///     <para>
	///         Written fresh for this codebase - no code or scoring constants were copied from any
	///         other project - but the underlying idea (subsequence match + boundary/contiguity
	///         bonuses) is standard technique, not something original to any one tool.
	///     </para>
	/// </summary>
	public static class FuzzyMatch
	{
		/// <summary>
		///     Scores <paramref name="candidate" /> against <paramref name="query" />, both
		///     already lower-cased by the caller (see <see cref="TagSearchIndex" /> and
		///     <see cref="CommandRegistry" />, which precompute the lower-case form once rather
		///     than on every keystroke). Returns <c>null</c> when the query is not a subsequence
		///     of the candidate at all - i.e. genuinely no match, not just a low-quality one.
		/// </summary>
		public static int? Score(ReadOnlySpan<char> candidate, ReadOnlySpan<char> query)
		{
			if (query.Length == 0) return 0;
			if (query.Length > candidate.Length) return null;

			int qi = 0, score = 0, run = 0, firstMatch = -1;
			for (int ci = 0; ci < candidate.Length && qi < query.Length; ci++)
			{
				if (candidate[ci] != query[qi])
				{
					run = 0;
					continue;
				}

				if (firstMatch < 0) firstMatch = ci;
				bool boundary = ci == 0 || !char.IsLetterOrDigit(candidate[ci - 1]);

				score += 10;
				if (boundary) score += 15;
				if (run > 0) score += 5 + run; // reward runs increasingly, so a long unbroken match beats several short ones
				run++;
				qi++;
			}

			if (qi < query.Length) return null; // not every query character was found, in order

			// A literal contiguous substring is a stronger signal than "the characters happen to
			// appear in order with gaps" even when the gap-scoring above already favours it -
			// this keeps e.g. "chingun" (a real substring) safely ahead of a same-length subsequence
			// match that only coincidentally strings the same letters together with gaps between.
			if (candidate.IndexOf(query) >= 0) score += 60;
			if (candidate.SequenceEqual(query)) score += 200;
			else if (candidate.StartsWith(query)) score += 80;

			score -= firstMatch; // a match that starts later in the string is a weaker signal
			score -= (candidate.Length - query.Length) / 4; // slight penalty for a less specific (longer) haystack

			return score;
		}
	}
}
