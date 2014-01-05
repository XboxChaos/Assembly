using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.IO;

namespace Blamite.Blam.Shaders
{
	/// <summary>
	/// Reads shader data from cache files.
	/// </summary>
	public interface IShaderStreamer
	{
		/// <summary>
		/// Reads a shader from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the beginning of the shader pointer.</param>
		/// <param name="type">The type of shader to read.</param>
		/// <returns>The shader that was read, or <c>null</c> if reading failed.</returns>
		IShader ReadShader(IReader reader, ShaderType type);

		/// <summary>
		/// Reads a shader from a stream and then serializes it into a byte array which can then be exported.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the beginning of the shader pointer.</param>
		/// <param name="type">The type of shader to read.</param>
		/// <returns>The serialized shader data, or <c>null</c> if reading failed.</returns>
		byte[] ExportShader(IReader reader, ShaderType type);

		/// <summary>
		/// Deserializes a serialized shader and injects it into the cache file.
		/// </summary>
		/// <param name="serializedShader">The serialized shader data to inject.</param>
		/// <param name="stream">The stream to manipulate. It should be positioned where the shader pointer should be written.</param>
		/// <returns><c>true</c> if the shader was successfully deserialized and injected.</returns>
		bool ImportShader(byte[] serializedShader, IStream stream);
	}
}
