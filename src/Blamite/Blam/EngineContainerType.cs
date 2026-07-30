namespace Blamite.Blam
{
	/// <summary>
	///     How a build's on-disk data is packaged - orthogonal to <see cref="EngineType" />, which
	///     says which Blam generation reads that data once it is found.
	/// </summary>
	/// <remarks>
	///     Every engine before Campaign Evolved ships a single monolithic cache file with a
	///     "head"/"daeh" (or Halo Trial's "Ehed"/"dehE") magic header, which is what
	///     <see cref="CacheFileLoader.FindEngineDescriptions" /> and
	///     <see cref="Serialization.EngineDescription.LoadCrucialLayoutInfo" /> assume by default.
	///     Campaign Evolved instead ships a set of Unreal Engine IoStore .utoc/.ucas container pairs
	///     with none of that - no "header" layout, no embedded build string - so both of those need
	///     a way to know, ahead of time, that a build's detection and validation rules are different.
	///     An engine declares this with <c>&lt;engineInfo&gt;&lt;container&gt;iostore&lt;/container&gt;</c>;
	///     the default, matching every engine that predates Campaign Evolved, is <see cref="Cache" />.
	/// </remarks>
	public enum EngineContainerType
	{
		/// <summary>
		///     A single cache file, identified by a "head"/"daeh" (or Halo Trial "Ehed"/"dehE") magic
		///     and validated by matching an embedded build string. Every engine before Campaign
		///     Evolved uses this.
		/// </summary>
		Cache,

		/// <summary>
		///     One or more Unreal Engine IoStore .utoc/.ucas container pairs, identified by the
		///     .utoc magic and a version byte, with no single build string to match. Used by
		///     Campaign Evolved.
		/// </summary>
		IoStore
	}
}
