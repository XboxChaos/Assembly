using System;
using Blamite.Blam.Resources.BSP;
using Blamite.Blam.Resources.Models;
using Blamite.Blam.Resources.Sounds;
using Blamite.IO;

namespace Blamite.Blam.Resources
{
	/// <summary>
	///     Provides methods for loading resource metadata from tags.
	/// </summary>
	public interface IResourceMetaLoader
	{
		/// <summary>
		///     Gets whether or not renderable model metadata can be loaded with <see cref="LoadRenderModelMeta" />.
		/// </summary>
		bool SupportsRenderModels { get; }

		/// <summary>
		///     Gets whether or not scenario BSP metadata can be loaded with <see cref="LoadScenarioBspMeta" />.
		/// </summary>
		bool SupportsScenarioBsps { get; }

		/// <summary>
		///     Gets whether or not sound metadata can be loaded with <see cref="LoadSoundMeta" />.
		/// </summary>
		bool SupportsSounds { get; }

		/// <summary>
		///     Loads metadata for a renderable model from a mode tag.
		/// </summary>
		/// <param name="modeTag">The mode tag to load metadata from.</param>
		/// <param name="reader">The reader to read the data with.</param>
		/// <returns>An <see cref="IRenderModel" /> object holding the metadata in the tag. Can be null if loading failed.</returns>
		/// <exception cref="ArgumentException">Thrown if modeTag points to null data or is not from the mode class.</exception>
		/// <exception cref="NotSupportedException">Thrown if loading renderable model metadata is not supported.</exception>
		IRenderModel LoadRenderModelMeta(ITag modeTag, IReader reader);

		/// <summary>
		///     Loads metadata for a scenario BSP from an sbsp tag.
		/// </summary>
		/// <param name="sbspTag">The sbsp tag to load metadata from.</param>
		/// <param name="reader">The reader to read the data with.</param>
		/// <returns>An <see cref="IScenarioBSP" /> object holding the metadata in the tag. Can be null if loading failed.</returns>
		/// <exception cref="ArgumentException">Thrown if sbspTag points to null data or is not from the sbsp class.</exception>
		/// <exception cref="NotSupportedException">Thrown if loading scenario BSP metadata is not supported.</exception>
		IScenarioBSP LoadScenarioBspMeta(ITag sbspTag, IReader reader);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sndTag"></param>
		/// <param name="reader"></param>
		/// <returns></returns>
		ISound LoadSoundMeta(ITag sndTag, IReader reader);
	}
}