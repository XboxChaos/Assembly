namespace Blamite.Blam
{
	/// <summary>
	///     Blam engine types.
	/// </summary>
	public enum EngineType
	{
		FirstGeneration, // H1
		SecondGeneration, // H2
		ThirdGeneration, // H3, Reach, H4
		Eldorado,

		// Append only - ordered comparisons like "Engine < EngineType.SecondGeneration" exist
		// elsewhere, so inserting a value here would silently renumber every value after it.
		FifthGeneration // Halo: Campaign Evolved. The Halo 5 / Infinite .module era is the natural
		                // fourth generation and precedes this game, so FourthGeneration stays reserved.
	}
}