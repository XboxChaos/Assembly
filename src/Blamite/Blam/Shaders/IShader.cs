using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam.Shaders
{
	/// <summary>
	/// A vertex or pixel shader.
	/// </summary>
	public interface IShader
	{
		/// <summary>
		/// Gets the shader's type.
		/// </summary>
		ShaderType Type { get; }

		/// <summary>
		/// Gets the shader's microcode data.
		/// </summary>
		byte[] Microcode { get; }

		/// <summary>
		/// Gets the path to the shader's UPDB (microcode program database) file.
		/// </summary>
		string DatabasePath { get; }
	}
}
