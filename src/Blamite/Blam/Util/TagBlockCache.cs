using System.Collections.Generic;
using System.Linq;

namespace Blamite.Blam.Util
{
	/// <summary>
	///     Contains a collection of blocks which can have be looked up and reused.
	/// </summary>
	/// <typeparam name="ElementType">The type of each entry in the block. This type must implement Equals.</typeparam>
	public class TagBlockCache<ElementType>
	{
		private readonly Dictionary<int, List<CachedTagBlock>> _blocksBySize =
			new Dictionary<int, List<CachedTagBlock>>();

		/// <summary>
		///     Adds a tag block to the cache.
		/// </summary>
		/// <param name="address">The address of the tag block.</param>
		/// <param name="elements">The elements in the tag block.</param>
		public void Add(long address, ICollection<ElementType> elements)
		{
			List<CachedTagBlock> blocks;
			if (!_blocksBySize.TryGetValue(elements.Count, out blocks))
			{
				blocks = new List<CachedTagBlock>();
				_blocksBySize[elements.Count] = blocks;
			}

			var block = new CachedTagBlock(address, elements);
			blocks.Add(block);
		}

		/// <summary>
		///     Checks if a block has been cached and retrieves its address if it is.
		/// </summary>
		/// <param name="block">The block to search for.</param>
		/// <param name="address">The variable to store the block's address to on success.</param>
		/// <returns>true if the block was cached and its address was retrieved.</returns>
		public bool TryGetAddress(ICollection<ElementType> block, out long address)
		{
			address = 0;

			// Find all blocks that have the given size
			List<CachedTagBlock> blocks;
			if (!_blocksBySize.TryGetValue(block.Count, out blocks))
				return false;

			// Compare the elements of each block
			foreach (CachedTagBlock cached in blocks)
			{
				if (cached.Elements.SequenceEqual(block))
				{
					address = cached.Address;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		///     A cached block.
		/// </summary>
		private class CachedTagBlock
		{
			/// <summary>
			///     Initializes a new instance of the <see cref="TagBlockCache{EntryType}.CachedTagBlock" /> class.
			/// </summary>
			/// <param name="address">The address of the block.</param>
			/// <param name="entries">The elements in the block.</param>
			public CachedTagBlock(long address, ICollection<ElementType> elements)
			{
				Address = address;
				Elements = elements;
			}

			/// <summary>
			///     Gets the address of the cached block.
			/// </summary>
			public long Address { get; private set; }

			/// <summary>
			///     Gets the elements in the cached block.
			/// </summary>
			public ICollection<ElementType> Elements { get; private set; }
		}
	}
}