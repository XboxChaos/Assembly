using Blamite.IO.IoStore;

namespace Blamite.Blam.FifthGen.Structures
{
	/// <summary>
	///     One IoStore container mounted as part of a Campaign Evolved tag namespace: a single
	///     <c>.utoc</c>/<c>.ucas</c> pair, opened and kept open for the lifetime of the
	///     <see cref="FifthGen.FifthGenTagTable" /> that mounted it.
	/// </summary>
	/// <remarks>
	///     A Campaign Evolved "cache file" is a whole folder of these - a mod's several <c>_P</c>
	///     override sets, or the shipped game's 28 <c>pakchunkN</c> pairs - not a single file (see
	///     <c>ce-format-spec.md</c> section 0 and <see cref="FifthGen.FifthGenTagTable" />). This
	///     wraps one member of that set together with the <c>.utoc</c> path it was opened from, which
	///     is the only per-container identity a mounted container otherwise has: nothing in the
	///     container itself names it, so recovering "which container is this" for diagnostics or for
	///     the <see cref="FifthGenNameOrigin.ContainerFilename" /> naming fallback means keeping the
	///     path around.
	/// </remarks>
	public class FifthGenMountedContainer
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="FifthGenMountedContainer" /> class.
		/// </summary>
		/// <param name="tocPath">The path the container's <c>.utoc</c> was opened from.</param>
		/// <param name="container">The opened container.</param>
		public FifthGenMountedContainer(string tocPath, IoStoreContainer container)
		{
			TocPath = tocPath;
			Container = container;
		}

		/// <summary>
		///     Gets the path the container's <c>.utoc</c> was opened from.
		/// </summary>
		public string TocPath { get; private set; }

		/// <summary>
		///     Gets the opened container.
		/// </summary>
		public IoStoreContainer Container { get; private set; }
	}
}
