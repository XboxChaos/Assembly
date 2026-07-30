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

		/// <summary>
		///     Replaces the open container this wraps with a different one, disposing the one being replaced.
		/// </summary>
		/// <remarks>
		///     Used around <see cref="Blamite.IO.IoStore.IoStoreContainerWriter" /> rewriting this container's
		///     <c>.utoc</c>/<c>.ucas</c> pair on disk: the streams this instance already has open have to be closed
		///     before the rewrite (it deletes and replaces those very files) and a fresh container opened on what is on
		///     disk afterward, rather than left stale for the rest of the mounted namespace's lifetime. Two separate
		///     calls - <c>SetContainer(null)</c> to close, then <c>SetContainer(freshlyOpened)</c> after the rewrite -
		///     rather than one method that does both, because the container has to stay closed for the whole rewrite.
		/// </remarks>
		/// <param name="container">The container to switch to, or <c>null</c> to just close the current one.</param>
		internal void SetContainer(IoStoreContainer container)
		{
			Container?.Dispose();
			Container = container;
		}
	}
}
