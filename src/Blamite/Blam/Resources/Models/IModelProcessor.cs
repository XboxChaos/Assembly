namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Interface for a class which handles data read from models.
	/// </summary>
	public interface IModelProcessor : IVertexProcessor
	{
		/// <summary>
		///     Called when the data for a model is about to be read.
		/// </summary>
		/// <param name="model">The model which will be read.</param>
		void BeginModel(IModel model);

		/// <summary>
		///     Called after the data for a model has been read.
		/// </summary>
		/// <param name="model">The model which was read.</param>
		void EndModel(IModel model);

		/// <summary>
		///     Called when the vertices for a submesh are about to be read.
		/// </summary>
		/// <param name="submesh">The submesh whose vertices will be read.</param>
		void BeginSubmeshVertices(IModelSubmesh submesh);

		/// <summary>
		///     Called after the vertices for a submesh have been read.
		/// </summary>
		/// <param name="submesh">The submesh whose vertices have been read.</param>
		void EndSubmeshVertices(IModelSubmesh submesh);

		/// <summary>
		///     Called when the index buffer for a submesh has been read.
		/// </summary>
		/// <param name="submesh">The submesh whose index buffer was read.</param>
		/// <param name="indices">The vertex indices for the submesh.</param>
		/// <param name="baseIndex">The index of the first vertex in the submesh.</param>
		void ProcessSubmeshIndices(IModelSubmesh submesh, ushort[] indices, int baseIndex);
	}
}