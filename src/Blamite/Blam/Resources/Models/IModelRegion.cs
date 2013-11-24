namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A region in a renderable model which can be customized by applying a permutation to it.
	/// </summary>
	public interface IModelRegion
	{
		/// <summary>
		///     Gets the name of the model region.
		/// </summary>
		StringID Name { get; }

		/// <summary>
		///     Gets the available permutations for the region.
		/// </summary>
		IModelPermutation[] Permutations { get; }
	}
}