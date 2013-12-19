using System.Collections.Generic;
using Blamite.Blam;

namespace Blamite.Injection
{
	/// <summary>
	///     Contains information about an address in a <see cref="DataBlock" /> which needs to be changed to point to
	///     newly-injected data.
	/// </summary>
	public class DataBlockAddressFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockAddressFixup" /> class.
		/// </summary>
		/// <param name="originalAddress">The original address of the data.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new address of the injected data to.</param>
		public DataBlockAddressFixup(uint originalAddress, int writeOffset)
		{
			OriginalAddress = originalAddress;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original address of the data that was pointed to.
		///     This can be used to find the data inside the tag container.
		/// </summary>
		public uint OriginalAddress { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new address of the injected data to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a tag reference in a <see cref="DataBlock" /> which needs to be changed to point to a
	///     tag in the target cache file.
	/// </summary>
	public class DataBlockTagFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockTagFixup" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the tag.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new datum index of the tag to.</param>
		public DataBlockTagFixup(DatumIndex originalIndex, int writeOffset)
		{
			OriginalIndex = originalIndex;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original datum index of the tag that was pointed to.
		///     This can be used to find the tag inside the tag container.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new datum index of the tag to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a resource reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a newly-injected resource.
	/// </summary>
	public class DataBlockResourceFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockResourceFixup" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the resouce.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new datum index of the resource to.</param>
		public DataBlockResourceFixup(DatumIndex originalIndex, int writeOffset)
		{
			OriginalIndex = originalIndex;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original datum index of the resource that was pointed to.
		///     This can be used to find the resource inside the tag container.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new datum index of the resource to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a stringID reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a stringID in the new cache file.
	/// </summary>
	public class DataBlockStringIDFixup
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlockStringIDFixup" /> class.
		/// </summary>
		/// <param name="originalString">The original string for the stringID.</param>
		/// <param name="writeOffset">The offset within the data block's data to write the new stringID to.</param>
		public DataBlockStringIDFixup(string originalString, int writeOffset)
		{
			OriginalString = originalString;
			WriteOffset = writeOffset;
		}

		/// <summary>
		///     Gets the original string for the stringID.
		/// </summary>
		public string OriginalString { get; private set; }

		/// <summary>
		///     Gets the offset within the data block's data to write the new stringID to.
		/// </summary>
		public int WriteOffset { get; private set; }
	}

	/// <summary>
	///     Contains information about a shader reference in a <see cref="DataBlock" /> which needs to be changed to point to
	///     a shader in the new cache file.
	/// </summary>
	public class DataBlockShaderFixup
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataBlockShaderFixup"/> class.
		/// </summary>
		/// <param name="writeOffset">The write offset.</param>
		/// <param name="data">The data.</param>
		public DataBlockShaderFixup(int writeOffset, byte[] data)
		{
			WriteOffset = writeOffset;
			Data = data;
		}

		/// <summary>
		/// Gets the offset to write the shader pointer to.
		/// </summary>
		public int WriteOffset { get; private set; }

		/// <summary>
		/// Gets the shader data to import.
		/// </summary>
		public byte[] Data { get; private set; }
	}

	/// <summary>
	///     Represents a block of tag data which can be injected into a cache file.
	/// </summary>
	public class DataBlock
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="DataBlock" /> class.
		/// </summary>
		/// <param name="originalAddress">The original address of the data block.</param>
		/// <param name="entryCount">The number of array entries stored by the block.</param>
		/// <param name="align">The power of two to align the block on.</param>
		/// <param name="data">The data to inject for the block.</param>
		public DataBlock(uint originalAddress, int entryCount, int align, byte[] data)
		{
			OriginalAddress = originalAddress;
			EntryCount = entryCount;
			Alignment = align;
			Data = data;
			AddressFixups = new List<DataBlockAddressFixup>();
			TagFixups = new List<DataBlockTagFixup>();
			ResourceFixups = new List<DataBlockResourceFixup>();
			StringIDFixups = new List<DataBlockStringIDFixup>();
			ShaderFixups = new List<DataBlockShaderFixup>();
		}

		/// <summary>
		///     Gets the original address of the data block (can be used to uniquely refer to it).
		/// </summary>
		public uint OriginalAddress { get; private set; }

		/// <summary>
		///     Gets the number of array entries stored by the data block (always 1 except for tag block data).
		/// </summary>
		public int EntryCount { get; private set; }

		/// <summary>
		///     Gets the size of each array entry stored in the data block.
		/// </summary>
		public int EntrySize
		{
			get { return Data.Length/EntryCount; }
		}

		/// <summary>
		/// Gets the power of two to align the block on.
		/// </summary>
		public int Alignment { get; private set; }

		/// <summary>
		///     Gets the data to inject for the block.
		/// </summary>
		public byte[] Data { get; private set; }

		/// <summary>
		///     Gets a list of memory addresses in this block that need to be changed in order to correctly point to other injected
		///     blocks.
		/// </summary>
		public List<DataBlockAddressFixup> AddressFixups { get; private set; }

		/// <summary>
		///     Gets a list of tag references in this block that need to be changed in order to correctly point to tags in the new
		///     cache file.
		/// </summary>
		public List<DataBlockTagFixup> TagFixups { get; private set; }

		/// <summary>
		///     Gets a list of resource references in this block that need to be changed in order to correctly point to injected
		///     resources.
		/// </summary>
		public List<DataBlockResourceFixup> ResourceFixups { get; private set; }

		/// <summary>
		///     Gets a list of stringIDs in this block that need to be changed in order to correctly point to strings in the new
		///     cache file.
		/// </summary>
		public List<DataBlockStringIDFixup> StringIDFixups { get; private set; }

		/// <summary>
		///     Gets a list of shader references in this block that need to be changed in order to correctly point to injected
		///     shaders.
		/// </summary>
		public List<DataBlockShaderFixup> ShaderFixups { get; private set; }
	}
}