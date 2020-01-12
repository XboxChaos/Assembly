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

		IZoneSet RequiredMapVariantsZoneSet { get; }
		IZoneSet SandboxMapVariantsZoneSet { get; }

		IZoneSet[] GeneralZoneSets { get; }
		IZoneSet[] BSPZoneSets { get; }
		IZoneSet[] BSPZoneSets2 { get; }
		IZoneSet[] BSPZoneSets3 { get; }
		IZoneSet[] CinematicZoneSets { get; }
		IZoneSet[] ScenarioZoneSets { get; }

		/// <summary>
		///		Adjusts the length of the resource arrays for all possible zonesets to fit the given index, if necessary.
		/// </summary>
		/// <param name="index">The index to adjust for.</param>
		void ExpandAllResources(int index);

		/// <summary>
		///		Adjusts the length of the tag arrays for all possible zonesets to fit the given index, if necessary.
		/// </summary>
		/// <param name="index">The index to adjust for.</param>
		void ExpandAllTags(int index);

		/// <summary>
		///     Saves changes made to zone sets in the table.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		void SaveChanges(IStream stream);
	}
}