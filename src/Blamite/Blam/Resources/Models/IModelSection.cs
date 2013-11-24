namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Types of extra per-vertex elements that a model section can have.
	/// </summary>
	public enum ExtraVertexElementType
	{
		Float3,
		None,
		Byte
	}

	/// <summary>
	///     A section of a model which can be optionally drawn based upon the currently active permutations.
	/// </summary>
	public interface IModelSection
	{
		/// <summary>
		///     Gets an array of submeshes for the section.
		/// </summary>
		IModelSubmesh[] Submeshes { get; }

		/// <summary>
		///     Gets an array of vertex groups in the section.
		/// </summary>
		IModelVertexGroup[] VertexGroups { get; }

		/// <summary>
		///     The vertex format index for the section's vertex buffer.
		/// </summary>
		/// <remarks>
		///     This is heavily dependent upon the cache file's target engine.
		/// </remarks>
		int VertexFormat { get; }

		/// <summary>
		///     Gets the number of extra elements per vertex, which immediately follow the vertex buffer.
		/// </summary>
		/// <seealso cref="ExtraElementsType" />
		int ExtraElementsPerVertex { get; }

		/// <summary>
		///     Gets the type of each extra per-vertex element.
		/// </summary>
		/// <remarks>
		///     No clue what these mean (lighting/transparency related?), but they have to be skipped over in order to process
		///     models correctly.
		/// </remarks>
		/// <seealso cref="ExtraElementsPerVertex" />
		ExtraVertexElementType ExtraElementsType { get; }
	}
}