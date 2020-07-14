using System.Collections.Generic;

namespace Blamite.Injection
{
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
		public DataBlock(uint originalAddress, int entryCount, int align, bool sort, byte[] data)
		{
			OriginalAddress = originalAddress;
			EntryCount = entryCount;
			Alignment = align;
			Sortable = sort;
			Data = data;
			AddressFixups = new List<DataBlockAddressFixup>();
			TagFixups = new List<DataBlockTagFixup>();
			ResourceFixups = new List<DataBlockResourceFixup>();
			StringIDFixups = new List<DataBlockStringIDFixup>();
			ShaderFixups = new List<DataBlockShaderFixup>();
			UnicListFixups = new List<DataBlockUnicListFixup>();
			InteropFixups = new List<DataBlockInteropFixup>();
			EffectFixups = new List<DataBlockEffectFixup>();
			SoundFixups = new List<DataBlockSoundFixup>();
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
		/// Gets whether or not this block needs sorting.
		/// </summary>
		public bool Sortable { get; private set; }

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

		/// <summary>
		/// Gets a list of multilingual unicode string lists in the block that need to be injected into the cache file.
		/// </summary>
		public List<DataBlockUnicListFixup> UnicListFixups { get; private set; }

		/// <summary>
		/// Gets a list of interops in the block that need to be injected into the cache file.
		/// </summary>
		public List<DataBlockInteropFixup> InteropFixups { get; private set; }

		/// <summary>
		/// Gets a list of compiled effect interops in the block that need to be injected into the cache file.
		/// </summary>
		public List<DataBlockEffectFixup> EffectFixups { get; private set; }

		/// <summary>
		/// Gets a list of sound gestalt information in the block that need to be injected into the cache file.
		/// </summary>
		public List<DataBlockSoundFixup> SoundFixups { get; private set; }
	}
}
