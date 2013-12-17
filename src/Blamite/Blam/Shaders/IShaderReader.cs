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
	public interface IShaderReader
	{
		/// <summary>
		/// Reads a shader from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the beginning of the shader pointer.</param>
		/// <param name="type">The type of shader to read.</param>
		/// <returns>The shader that was read, or <c>null</c> if reading failed.</returns>
		IShader ReadShader(IReader reader, ShaderType type);
	}
}
