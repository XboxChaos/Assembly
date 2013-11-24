using Blamite.IO;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     A table of zone sets loaded from a cache file.
	/// </summary>
	public interface IZoneSetTable
	{
		/// <summary>
		///     Gets the global zone set. This takes priority over all other zone sets.
		/// </summary>
		IZoneSet GlobalZoneSet { get; }

		IZoneSet UnattachedZoneSet { get; }
		IZoneSet DiscForbiddenZoneSet { get; }
		IZoneSet DiscAlwaysStreamingZoneSet { get; }

		IZoneSet[] GeneralZoneSets { get; }
		IZoneSet[] BSPZoneSets { get; }
		IZoneSet[] BSPZoneSets2 { get; }
		IZoneSet[] BSPZoneSets3 { get; }
		IZoneSet[] CinematicZoneSets { get; }
		IZoneSet[] CustomZoneSets { get; }

		/// <summary>
		///     Saves changes made to zone sets in the table.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		void SaveChanges(IStream stream);
	}
}