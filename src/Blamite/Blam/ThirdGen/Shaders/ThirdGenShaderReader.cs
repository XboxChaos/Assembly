using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Blam.Shaders;
using Blamite.Flexibility;
using Blamite.IO;

namespace Blamite.Blam.ThirdGen.Shaders
{
	/// <summary>
	/// Reads shader data from third-gen cache files.
	/// </summary>
	class ThirdGenShaderReader : IShaderReader
	{
		private ICacheFile _cacheFile;
		private StructureLayout _pixelShaderInfoLayout;
		private StructureLayout _vertexShaderInfoLayout;
		private StructureLayout _debugInfoLayout;
		private StructureLayout _updbPointerLayout;
		private StructureLayout _codeInfoLayout;

		public ThirdGenShaderReader(ICacheFile cacheFile, EngineDescription desc)
		{
			_cacheFile = cacheFile;
			if (desc.Layouts.HasLayout("pixel shader info"))
				_pixelShaderInfoLayout = desc.Layouts.GetLayout("pixel shader info");
			if (desc.Layouts.HasLayout("vertex shader info"))
				_vertexShaderInfoLayout = desc.Layouts.GetLayout("vertex shader info");
			if (desc.Layouts.HasLayout("shader debug info"))
				_debugInfoLayout = desc.Layouts.GetLayout("shader debug info");
			if (desc.Layouts.HasLayout("updb pointer"))
				_updbPointerLayout = desc.Layouts.GetLayout("updb pointer");
			if (desc.Layouts.HasLayout("code info"))
				_codeInfoLayout = desc.Layouts.GetLayout("code info");
		}

		/// <summary>
		/// Reads a shader from a stream.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the beginning of the shader pointer.</param>
		/// <param name="type">The type of shader to read.</param>
		/// <returns>
		/// The shader that was read, or <c>null</c> if reading failed.
		/// </returns>
		public IShader ReadShader(IReader reader, ShaderType type)
		{
			if (_debugInfoLayout == null)
				return null;
			var infoLayout = (type == ShaderType.Pixel) ? _pixelShaderInfoLayout : _vertexShaderInfoLayout;
			if (infoLayout == null)
				return null;

			// Read the shader info address and seek to it
			var infoAddr = reader.ReadUInt32();
			if (infoAddr == 0)
				return null;
			var infoOffset = _cacheFile.MetaArea.PointerToOffset(infoAddr);
			reader.SeekTo(infoOffset);

			// Read the info structure
			var infoValues = StructureReader.ReadStructure(reader, infoLayout);

			// Read the debug info
			// TODO: Some shaders have craploads of padding before this - figure out how to handle that
			var debugInfoOffset = reader.Position;
			var debugValues = StructureReader.ReadStructure(reader, _debugInfoLayout);

			// Do a quick check on the magic to verify that the debug info is valid
			if (debugValues.GetIntegerOrDefault("magic", 0) >> 16 != 0x102A)
				return null;

			// Read the UPDB path
			var updbPath = "";
			if (_updbPointerLayout != null)
			{
				var updbPointerOffset = debugValues.GetIntegerOrDefault("updb pointer offset", 0);
				if (updbPointerOffset != 0)
				{
					reader.SeekTo(debugInfoOffset + updbPointerOffset);
					var updbValues = StructureReader.ReadStructure(reader, _updbPointerLayout);
					var pathLength = (int)updbValues.GetIntegerOrDefault("string length", 0);
					if (pathLength > 0)
						updbPath = reader.ReadAscii(pathLength);
				}
			}

			var totalSize = debugValues.GetIntegerOrDefault("shader data size", 0); 
			var constantSize = 0U;
			var codeSize = totalSize;

			// If code info is present, then use that to determine the shader data layout
			if (_codeInfoLayout != null)
			{
				var codeInfoOffset = debugValues.GetIntegerOrDefault("code info offset", 0);
				if (codeInfoOffset != 0)
				{
					reader.SeekTo(debugInfoOffset + codeInfoOffset);
					var codeInfoValues = StructureReader.ReadStructure(reader, _codeInfoLayout);
					constantSize = codeInfoValues.GetIntegerOrDefault("constant data size", 0);
					codeSize = codeInfoValues.GetIntegerOrDefault("code data size", 0);
				}
			}

			// Now read the microcode data
			if (codeSize == 0)
				return null;
			var dataAddr = infoValues.GetIntegerOrDefault("shader data address", 0);
			if (dataAddr == 0)
				return null;
			var dataOffset = _cacheFile.MetaArea.PointerToOffset(dataAddr);
			reader.SeekTo(dataOffset + constantSize);
			var microcode = reader.ReadBlock((int)codeSize);

			return new ThirdGenShader(type, microcode, updbPath);
		}
	}
}
