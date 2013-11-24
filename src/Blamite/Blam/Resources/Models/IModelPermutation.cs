namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A permutation for a region in a model.
	/// </summary>
	/// <seealso cref="IModelRegion" />
	public interface IModelPermutation
	{
		/// <summary>
		///     Gets the stringID for the name of the permutation.
		/// </summary>
		StringID Name { get; }

		/// <summary>
		///     Gets the index of the model section to draw for the permutation.
		///     Can be -1.
		/// </summary>
		int ModelSectionIndex { get; }
	}
}