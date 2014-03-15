using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Resources;

namespace Blamite.Injection
{
	/// <summary>
	///     A file containing information about tags and resources which were extracted from a cache file
	///     and which can be injected into a new one.
	/// </summary>
	public class TagContainer
	{
		private readonly Dictionary<uint, DataBlock> _dataBlocksByAddress = 
			new Dictionary<uint, DataBlock>();

		private readonly Dictionary<int, ResourcePage> _pagesByIndex = 
			new Dictionary<int, ResourcePage>();

		private readonly Dictionary<DatumIndex, ExtractedResourceInfo> _resourcesByIndex =
			new Dictionary<DatumIndex, ExtractedResourceInfo>();

		private readonly Dictionary<int, ExtractedPage> _extractedResourcePageByPageIndex =
			new Dictionary<int, ExtractedPage>();

		private readonly Dictionary<DatumIndex, ExtractedTag> _tagsByIndex = 
			new Dictionary<DatumIndex, ExtractedTag>();

		/// <summary>
		///     Gets a collection of all data blocks in the container.
		/// </summary>
		public ICollection<DataBlock> DataBlocks
		{
			get { return _dataBlocksByAddress.Values; }
		}

		/// <summary>
		///     Gets a collection of all tags in the container.
		/// </summary>
		public ICollection<ExtractedTag> Tags
		{
			get { return _tagsByIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all resource page information objects in the container.
		/// </summary>
		public ICollection<ResourcePage> ResourcePages
		{
			get { return _pagesByIndex.Values; }
		}

		/// <summary>
		///     
		/// </summary>
		public ICollection<ExtractedPage> ExtractedResourcePages
		{
			get { return _extractedResourcePageByPageIndex.Values; }
		}

		/// <summary>
		///     Gets a collection of all resource information objects in the container.
		/// </summary>
		public ICollection<ExtractedResourceInfo> Resources
		{
			get { return _resourcesByIndex.Values; }
		}

		/// <summary>
		///     Adds a data block to the container.
		/// </summary>
		/// <param name="block">The block to add.</param>
		public void AddDataBlock(DataBlock block)
		{
			_dataBlocksByAddress[block.OriginalAddress] = block;
		}

		/// <summary>
		///     Adds information about an extracted tag to the container.
		/// </summary>
		/// <param name="tag">The tag information to add.</param>
		public void AddTag(ExtractedTag tag)
		{
			_tagsByIndex[tag.OriginalIndex] = tag;
		}

		/// <summary>
		///     Adds information about a resource page to the container.
		/// </summary>
		/// <param name="page">The page to add.</param>
		public void AddResourcePage(ResourcePage page)
		{
			_pagesByIndex[page.Index] = page;
		}

		/// <summary>
		///     
		/// </summary>
		/// <param name="extractedPage"></param>
		/// <param name="page"></param>
		public void AddExtractedResourcePage(ExtractedPage extractedPage)
		{
			_extractedResourcePageByPageIndex[extractedPage.ResourcePageIndex] = extractedPage;
		}

		/// <summary>
		///     Adds information about a resource to the container.
		/// </summary>
		/// <param name="resource">The resource to add.</param>
		public void AddResource(ExtractedResourceInfo resource)
		{
			_resourcesByIndex[resource.OriginalIndex] = resource;
		}

		/// <summary>
		///     Finds the data block which has a specified original address.
		/// </summary>
		/// <param name="originalAddress">The original address of the data block to find.</param>
		/// <returns>The <see cref="DataBlock" /> with the original address.</returns>
		public DataBlock FindDataBlock(uint originalAddress)
		{
			return _dataBlocksByAddress[originalAddress];
		}

		/// <summary>
		///     Finds the tag which has a specified original datum index.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the tag to find.</param>
		/// <returns>The <see cref="ExtractedTag" /> with the original datum index.</returns>
		public ExtractedTag FindTag(DatumIndex originalIndex)
		{
			return _tagsByIndex[originalIndex];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="originalIndex"></param>
		/// <returns></returns>
		public ExtractedPage FindExtractedResourcePage(int originalIndex)
		{
			return _extractedResourcePageByPageIndex[originalIndex];
		}

		/// <summary>
		///     Finds the resource page which has a specified original index.
		/// </summary>
		/// <param name="originalIndex">The original index of the resource page to find.</param>
		/// <returns>The <see cref="ResourcePage" /> with the original index.</returns>
		public ResourcePage FindResourcePage(int originalIndex)
		{
			return _pagesByIndex[originalIndex];
		}

		/// <summary>
		///     Finds the resource which has a specified original datum index.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the resource to find.</param>
		/// <returns>The <see cref="ExtractedResourceInfo" /> with the original datum index.</returns>
		public ExtractedResourceInfo FindResource(DatumIndex originalIndex)
		{
			return _resourcesByIndex[originalIndex];
		}
	}
}