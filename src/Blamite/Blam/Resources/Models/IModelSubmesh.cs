namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     A submesh for a model section.
	/// </summary>
	public interface IModelSubmesh
	{
		/// <summary>
		///     Gets the index of the shader to render the submesh with.
		/// </summary>
		int ShaderIndex { get; }

		/// <summary>
		///     Gets the index of the first vertex in the index buffer.
		/// </summary>
		int IndexBufferStart { get; }

		/// <summary>
		///     Gets the number of vertices in the index buffer.
		/// </summary>
		int IndexBufferCount { get; }

		/// <summary>
		///     Gets the index of the first vertex group in the submesh.
		/// </summary>
		int VertexGroupStart { get; }

		/// <summary>
		///     Gets the number of vertex groups in the submesh.
		/// </summary>
		int VertexGroupCount { get; }

		/// <summary>
		///     Gets the number of vertices in the vertex buffer.
		/// </summary>
		int VertexBufferCount { get; }
	}
}