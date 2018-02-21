using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Shaders;
using Blamite.Serialization;

namespace Blamite.Blam.FourthGen.Shaders
{
	/// <summary>
	/// Fourth-gen implementation of <see cref="IShader"/>.
	/// </summary>
	class FourthGenShader : IShader
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FourthGenShader"/> class.
		/// The shader will be initialized with a microcode buffer and a database path.
		/// </summary>
		/// <param name="type">The shader's type.</param>
		/// <param name="microcode">The microcode.</param>
		/// <param name="dbPath">The UPDB path.</param>
		public FourthGenShader(ShaderType type, byte[] microcode, string dbPath)
		{
			Type = type;
			Microcode = microcode;
			DatabasePath = dbPath;
		}

		/// <summary>
		/// Gets the shader's type.
		/// </summary>
		public ShaderType Type { get; private set; }

		/// <summary>
		/// Gets the shader's microcode data.
		/// </summary>
		public byte[] Microcode { get; private set; }

		/// <summary>
		/// Gets the path to the shader's UPDB (microcode program database) file.
		/// </summary>
		public string DatabasePath { get; private set; }
	}
}
