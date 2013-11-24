using System.Collections;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.Resources.Models
{
	/// <summary>
	///     Provides methods for reading model data.
	/// </summary>
	public static class ModelReader
	{
		/// <summary>
		///     Reads the resource data for a model from a stream and passes it to an IModelProcessor.
		/// </summary>
		/// <param name="reader">The stream to read the model data from.</param>
		/// <param name="model">The model's metadata.</param>
		/// <param name="sectionsToRead">
		///     A BitArray controlling which sections to read. Indices which are set to to true will be
		///     read.
		/// </param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="processor">The IModelProcessor to pass the read model data to.</param>
		public static void ReadModelData(IReader reader, IRenderModel model, BitArray sectionsToRead,
			EngineDescription buildInfo, IModelProcessor processor)
		{
			processor.BeginModel(model);

			ReadVertexBuffers(reader, model, sectionsToRead, buildInfo, processor);
			ReadIndexBuffers(reader, model, sectionsToRead, buildInfo, processor);

			processor.EndModel(model);
		}

		/// <summary>
		///     Reads the vertex buffers for a model from a stream and passes them to an IModelProcessor.
		/// </summary>
		/// <param name="reader">The stream to read the vertex buffers from.</param>
		/// <param name="model">The model's metadata.</param>
		/// <param name="sectionsToRead">
		///     A BitArray controlling which sections to read. Indices which are set to to true will be
		///     read.
		/// </param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="processor">The IModelProcessor to pass the read model data to.</param>
		private static void ReadVertexBuffers(IReader reader, IRenderModel model, BitArray sectionsToRead,
			EngineDescription buildInfo, IModelProcessor processor)
		{
			for (int i = 0; i < model.Sections.Length; i++)
				ReadSectionVertices(reader, model.Sections[i], model.BoundingBoxes[0], buildInfo,
					sectionsToRead[i] ? processor : null);
		}

		/// <summary>
		///     Reads the vertex buffer for a section in a model.
		/// </summary>
		/// <param name="reader">The stream to read the vertex buffer from.</param>
		/// <param name="section">The model section that the vertex buffer belongs to.</param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="boundingBox">The bounding box for the model section.</param>
		/// <param name="processor">
		///     The IModelProcessor to pass the read model data to, or null if the vertex buffer should be
		///     skipped over.
		/// </param>
		private static void ReadSectionVertices(IReader reader, IModelSection section, BoundingBox boundingBox,
			EngineDescription buildInfo, IModelProcessor processor)
		{
			VertexLayout layout = buildInfo.VertexLayouts.GetLayout(section.VertexFormat);

			foreach (IModelSubmesh submesh in section.Submeshes)
			{
				if (processor != null)
					processor.BeginSubmeshVertices(submesh);

				VertexBufferReader.ReadVertices(reader, layout, submesh.VertexBufferCount, boundingBox, processor);

				if (processor != null)
					processor.EndSubmeshVertices(submesh);
			}

			VertexBufferReader.SkipExtraElements(reader, section);
		}

		/// <summary>
		///     Reads the index buffers for a model from a stream and passes them to an IModelProcessor.
		/// </summary>
		/// <param name="reader">The stream to read the index buffers from.</param>
		/// <param name="model">The model's metadata.</param>
		/// <param name="sectionsToRead">
		///     A BitArray controlling which sections to read. Indices which are set to to true will be
		///     read.
		/// </param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="processor">The IModelProcessor to pass the read model data to.</param>
		private static void ReadIndexBuffers(IReader reader, IRenderModel model, BitArray sectionsToRead,
			EngineDescription buildInfo, IModelProcessor processor)
		{
			int baseIndex = 0;
			for (int i = 0; i < model.Sections.Length; i++)
			{
				IModelSection section = model.Sections[i];
				ReadSectionIndices(reader, section, baseIndex, buildInfo, sectionsToRead[i] ? processor : null);

				if (sectionsToRead[i])
					baseIndex += CountVertices(section);
			}
		}

		/// <summary>
		///     Reads the index buffer for a section in a model.
		/// </summary>
		/// <param name="reader">The stream to read the index buffer from.</param>
		/// <param name="section">The model section that the index buffer belongs to.</param>
		/// <param name="buildInfo">Information about the cache file's target engine.</param>
		/// <param name="processor">
		///     The IModelProcessor to pass the read model data to, or null if the index buffer should be
		///     skipped over.
		/// </param>
		private static void ReadSectionIndices(IReader reader, IModelSection section, int baseIndex,
			EngineDescription buildInfo, IModelProcessor processor)
		{
			foreach (IModelSubmesh submesh in section.Submeshes)
			{
				if (processor != null)
				{
					ushort[] indices = IndexBufferReader.ReadIndexBuffer(reader, submesh.IndexBufferCount);
					processor.ProcessSubmeshIndices(submesh, indices, baseIndex);
				}
				else
				{
					IndexBufferReader.SkipIndexBuffer(reader, submesh.IndexBufferCount);
				}
			}
			reader.SeekTo((reader.Position + 3) & ~3); // Align 4
		}

		/// <summary>
		///     Counts the total number of vertices in a model section's vertex buffer.
		/// </summary>
		/// <param name="section">The section to count vertices for.</param>
		/// <returns>The total number of vertices in the section's vertex buffer.</returns>
		private static int CountVertices(IModelSection section)
		{
			int totalVertices = 0;
			foreach (IModelSubmesh submesh in section.Submeshes)
				totalVertices += submesh.VertexBufferCount;
			return totalVertices;
		}
	}
}