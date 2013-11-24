namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A full model which can be drawn on the screen.
	/// </summary>
	public interface IRenderModel : IModel
	{
		/// <summary>
		///     Gets the permutation regions available for the model.
		/// </summary>
		IModelRegion[] Regions { get; }
	}
}