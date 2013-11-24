namespace Blamite.Blam.Resources.Models
{
	// TODO: figure out what these are actually used for...

	/// <summary>
	///     A group of vertices in a model.
	/// </summary>
	public interface IModelVertexGroup
	{
		/// <summary>
		///     Gets the index of the first vertex in the index buffer.
		/// </summary>
		int IndexBufferStart { get; }

		/// <summary>
		///     Gets the number of vertices in the index buffer.
		/// </summary>
		int IndexBufferCount { get; }

		/// <summary>
		///     Gets the number of vertices in the vertex buffer.
		/// </summary>
		int VertexBufferCount { get; }

		/// <summary>
		///     Gets the submesh that that the vertex group belongs to.
		/// </summary>
		IModelSubmesh Submesh { get; }
	}
}