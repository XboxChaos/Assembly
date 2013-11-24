namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A 3D model.
	/// </summary>
	public interface IModel
	{
		/// <summary>
		///     Gets the sections of the model.
		/// </summary>
		IModelSection[] Sections { get; }

		/// <summary>
		///     Gets the model's bounding boxes. Can be null.
		/// </summary>
		BoundingBox[] BoundingBoxes { get; }

		/// <summary>
		///     Gets the datum index of the model's resource.
		/// </summary>
		/// <seealso cref="IResourceManager" />
		DatumIndex ModelResourceIndex { get; }
	}
}