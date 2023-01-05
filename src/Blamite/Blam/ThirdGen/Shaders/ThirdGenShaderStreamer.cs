using System;
using System.IO;
using Blamite.Blam.Shaders;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.ThirdGen.Shaders
{
	/// <summary>
	/// Streams shader data in third-gen cache files.
	/// </summary>
	class ThirdGenShaderStreamer : IShaderStreamer
	{
		private ICacheFile _cacheFile;
		private StructureLayout _pixelShaderInfoLayout;
		private StructureLayout _vertexShaderInfoLayout;
		private StructureLayout _debugInfoLayout;
		private StructureLayout _updbPointerLayout;
		private StructureLayout _codeInfoLayout;

		public ThirdGenShaderStreamer(ICacheFile cacheFile, EngineDescription desc)
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
			var info = ReadShaderInfo(reader, type);
			if (info == null || info.CodeDataSize == 0)
				return null;
			
			// Read the code data
			var dataOffset = _cacheFile.MetaArea.PointerToOffset(info.DataAddress);
			reader.SeekTo(dataOffset + info.ConstantDataSize); // Code data comes after the constant data
			var microcode = reader.ReadBlock((int)info.CodeDataSize);

			return new ThirdGenShader(type, microcode, info.DatabasePath);
		}

		/// <summary>
		/// Reads a shader from a stream and then serializes it into a byte array which can then be exported.
		/// </summary>
		/// <param name="reader">The stream to read from. It should be positioned at the beginning of the shader pointer.</param>
		/// <param name="type">The type of shader to read.</param>
		/// <returns>
		/// The serialized shader data, or <c>null</c> if reading failed.
		/// </returns>
		public byte[] ExportShader(IReader reader, ShaderType type)
		{
			var info = ReadShaderInfo(reader, type);
			if (info == null)
				return null;

			using (var memStream = new MemoryStream())
			{
				using (var writer = new EndianWriter(memStream, Endian.BigEndian))
				{
					writer.WriteInt32(SerializationMagic);
					writer.WriteByte((byte)type);

					// Write the layout size for compatibility checking when deserializing
					if (type == ShaderType.Pixel)
						writer.WriteInt32(_pixelShaderInfoLayout.Size);
					else
						writer.WriteInt32(_vertexShaderInfoLayout.Size);

					// Write the raw debug info
					reader.SeekTo(info.DebugInfoOffset);
					writer.WriteUInt32(info.DebugInfoSize);
					writer.WriteBlock(reader.ReadBlock((int)info.DebugInfoSize));

					// Write the raw shader data
					var dataOffset = _cacheFile.MetaArea.PointerToOffset(info.DataAddress);
					reader.SeekTo(dataOffset);
					writer.WriteUInt32(info.DataSize);
					writer.WriteBlock(reader.ReadBlock((int)info.DataSize));

					// Create the result from the memory stream's buffer
					var result = new byte[memStream.Length];
					Buffer.BlockCopy(memStream.ToArray(), 0, result, 0, (int)memStream.Length);
					return result;
				}
			}
		}

		/// <summary>
		/// Deserializes a serialized shader and injects it into the cache file.
		/// </summary>
		/// <param name="serializedShader">The serialized shader data to inject.</param>
		/// <param name="stream">The stream to manipulate. It should be positioned where the shader pointer should be written.</param>
		/// <returns>
		///   <c>true</c> if the shader was successfully deserialized and injected.
		/// </returns>
		public bool ImportShader(byte[] serializedShader, IStream stream)
		{
			if (serializedShader == null || serializedShader.Length == 0)
			{
				// Null shader
				stream.WriteUInt32(0);
				return true;
			}

			var pointerOffset = stream.Position + _cacheFile.MetaArea.OffsetToPointer(0);
			using (var reader = new EndianReader(new MemoryStream(serializedShader), Endian.BigEndian))
			{
				// Check the magic
				if (reader.ReadInt32() != SerializationMagic)
					return false;

				// Read the shader type and determine which info layout to use
				var type = (ShaderType)reader.ReadByte();
				StructureLayout infoLayout = null;
				if (type == ShaderType.Pixel)
					infoLayout = _pixelShaderInfoLayout;
				else if (type == ShaderType.Vertex)
					infoLayout = _vertexShaderInfoLayout;
				if (infoLayout == null)
					return false;

				// Read and verify the layout size
				var infoLayoutSize = reader.ReadInt32();
				if (infoLayoutSize != infoLayout.Size)
					return false;

				// Read the raw debug info and data
				var debugInfoSize = reader.ReadUInt32();
				var debugInfo = reader.ReadBlock((int)debugInfoSize);
				var dataSize = reader.ReadUInt32();
				var data = reader.ReadBlock((int)dataSize);

				// Allocate space for the shader data and write it in
				var dataAddr = _cacheFile.Allocator.Allocate((uint)dataSize, 0x10, stream); // 16-byte aligned
				var dataOffset = _cacheFile.MetaArea.PointerToOffset(dataAddr);
				stream.SeekTo(dataOffset);
				stream.WriteBlock(data);

				// Allocate and zero space for the info structures
				var infoSize = (uint)infoLayoutSize + debugInfoSize;
				var infoAddr = _cacheFile.Allocator.Allocate(infoSize, 0x10, stream); // 16-byte aligned too
				var infoOffset = _cacheFile.MetaArea.PointerToOffset(infoAddr);
				stream.SeekTo(infoOffset);
				StreamUtil.Fill(stream, 0, infoSize);

				// Write the basic info structure
				stream.SeekTo(infoOffset);
				var infoValues = new StructureValueCollection();
				infoValues.SetInteger("shader data address", (uint)dataAddr);
				StructureWriter.WriteStructure(infoValues, infoLayout, stream);

				// Write the debug info structure
				stream.WriteBlock(debugInfo);

				// Finally, write the shader pointer
				stream.SeekTo(pointerOffset - _cacheFile.MetaArea.OffsetToPointer(0));
				stream.WriteUInt32((uint)infoAddr);
			}
			return true;
		}

		private ShaderInfo ReadShaderInfo(IReader reader, ShaderType type)
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

			// Do a quick check on the magic and size to verify that the debug info is valid
			if (debugValues.GetIntegerOrDefault("magic", 0) >> 16 != 0x102A)
				return null;
			var debugSize = (uint)debugValues.GetIntegerOrDefault("structure size", 0);
			if (debugSize == 0)
				return null;

			// Read the UPDB path
			var updbPath = "";
			if (_updbPointerLayout != null)
			{
				var updbPointerOffset = (uint)debugValues.GetIntegerOrDefault("updb pointer offset", 0);
				if (updbPointerOffset != 0)
				{
					reader.SeekTo(debugInfoOffset + updbPointerOffset);
					var updbValues = StructureReader.ReadStructure(reader, _updbPointerLayout);
					var pathLength = (int)updbValues.GetIntegerOrDefault("string length", 0);
					if (pathLength > 0)
						updbPath = reader.ReadAscii(pathLength);
				}
			}

			var totalSize = (uint)debugValues.GetIntegerOrDefault("shader data size", 0);
			var constantSize = 0U;
			var codeSize = totalSize;

			// If code info is present, then use that to determine the shader data layout
			if (_codeInfoLayout != null)
			{
				var codeInfoOffset = (uint)debugValues.GetIntegerOrDefault("code info offset", 0);
				if (codeInfoOffset != 0)
				{
					reader.SeekTo(debugInfoOffset + codeInfoOffset);
					var codeInfoValues = StructureReader.ReadStructure(reader, _codeInfoLayout);
					constantSize = (uint)codeInfoValues.GetIntegerOrDefault("constant data size", 0);
					codeSize = (uint)codeInfoValues.GetIntegerOrDefault("code data size", 0);
				}
			}
			
			// Grab the data address
			var dataAddr = (uint)infoValues.GetIntegerOrDefault("shader data address", 0);
			if (dataAddr == 0)
				return null;

			return new ShaderInfo
			{
				DataAddress = dataAddr,
				DebugInfoOffset = (uint)debugInfoOffset,
				DebugInfoSize = debugSize,
				DatabasePath = updbPath,
				DataSize = totalSize,
				ConstantDataSize = constantSize,
				CodeDataSize = codeSize
			};
		}

		private static int SerializationMagic = CharConstant.FromString("gen3");

		private class ShaderInfo
		{
			public uint DataAddress { get; set; }
			public uint DebugInfoOffset { get; set; }
			public uint DebugInfoSize { get; set; }
			public string DatabasePath { get; set; }
			public uint DataSize { get; set; }
			public uint ConstantDataSize { get; set; }
			public uint CodeDataSize { get; set; }
		}
	}
}
