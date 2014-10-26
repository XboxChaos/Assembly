using System;

namespace Blamite.Injection
{
	/// <summary>
	/// 
	/// </summary>
	public class ExtractedPage
	{
		/// <summary>
		/// 
		/// </summary>
		public ExtractedPage() { }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="extractedPageData"></param>
		/// <param name="resourcePageIndex"></param>
		public ExtractedPage(byte[] extractedPageData, Int32 resourcePageIndex)
		{
			ResourcePageIndex = resourcePageIndex;
			ExtractedPageData = extractedPageData;
		}
		
		/// <summary>
		/// 
		/// </summary>
		public Int32 ResourcePageIndex { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public byte[] ExtractedPageData { get; set; }
	}
}
