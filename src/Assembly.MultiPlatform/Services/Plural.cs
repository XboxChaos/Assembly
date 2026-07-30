using System.Globalization;

namespace Assembly.MultiPlatform.Services
{
	/// <summary>
	///     Formats "1 source" / "2 sources" instead of "1 source(s)".
	/// </summary>
	/// <remarks>
	///     Small, but it was visible three times in one screenshot - the toolbar counter, the
	///     empty-state hint and the status bar - which is enough to set a tone. "(s)" reads as
	///     something nobody came back to finish, and the fix is one branch, so there is no case
	///     for leaving it.
	///     <para>
	///         Diagnostic output (HeadlessProbe) is deliberately left alone: it is read by whoever
	///         ran the command, not by someone using the app, and churning those strings would
	///         invalidate every recorded expectation for no reader's benefit.
	///     </para>
	/// </remarks>
	public static class Plural
	{
		/// <summary>Count and noun, agreeing. Thousands are separated, because these run large.</summary>
		/// <param name="count">The quantity.</param>
		/// <param name="singular">The noun in its singular form, e.g. "source".</param>
		/// <param name="plural">The plural form, when it is not just <paramref name="singular" /> + "s".</param>
		public static string Of(int count, string singular, string? plural = null)
			=> count.ToString("N0", CultureInfo.CurrentCulture) + " " + Noun(count, singular, plural);

		/// <summary>The noun alone, agreeing with <paramref name="count" /> - for callers that
		/// have already written the number, or are binding it separately.</summary>
		public static string Noun(int count, string singular, string? plural = null)
			=> count == 1 ? singular : plural ?? singular + "s";
	}
}
