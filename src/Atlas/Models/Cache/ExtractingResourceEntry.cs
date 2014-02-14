using Blamite.Blam.Resources;
using Blamite.Injection;

namespace Atlas.Models.Cache
{
	public class ExtractingResourceEntry
	{
		public ExtractingResourceEntry(bool extract, Resource resource, ExtractedResourceInfo extractedResourceInfo)
		{
			Extract = extract;
			ExtractedResourceInfo = extractedResourceInfo;
			Resource = resource;
		}

		public bool Extract { get; set; }

		public Resource Resource { get; set; }

		public ExtractedResourceInfo ExtractedResourceInfo { get; set; }

		public string PrimaryPageFilePath
		{
			get
			{
				if (Resource == null || Resource.Location == null || Resource.Location.PrimaryPage == null)
					return "--";
				
				return Resource.Location.PrimaryPage.FilePath ?? "Local Resource";
			}
		}
		public string SecondaryPageFilePath
		{
			get
			{
				if (Resource == null || Resource.Location == null || Resource.Location.SecondaryPage == null)
					return "--";

				return Resource.Location.SecondaryPage.FilePath ?? "Local Resource";
			}
		}
	}
}
