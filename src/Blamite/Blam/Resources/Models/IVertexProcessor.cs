using Blamite.Flexibility;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Interface for a class which processes vertices read from a vertex buffer.
	/// </summary>
	/// <seealso cref="VertexBufferReader" />
	public interface IVertexProcessor
	{
		/// <summary>
		///     Called when a vertex is about to be read.
		/// </summary>
		void BeginVertex();

		/// <summary>
		///     Called after an element in a vertex has been read.
		/// </summary>
		/// <param name="x">The X component of the element.</param>
		/// <param name="y">The Y component of the element.</param>
		/// <param name="z">The Z component of the element.</param>
		/// <param name="w">The W component of the element.</param>
		/// <param name="layout">The element's layout.</param>
		void ProcessVertexElement(float x, float y, float z, float w, VertexElementLayout layout);

		/// <summary>
		///     Called after a vertex has been read.
		/// </summary>
		void EndVertex();
	}
}