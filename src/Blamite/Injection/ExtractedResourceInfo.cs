using System.Collections.Generic;
using Blamite.Blam;
using Blamite.Blam.Resources;

namespace Blamite.Injection
{
	/// <summary>
	///     Contains information about an extracted resource's location.
	/// </summary>
	/// <seealso cref="ExtractedResourceInfo" />
	public class ExtractedResourcePointer
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ExtractedResourcePointer" /> class.
		/// </summary>
		public ExtractedResourcePointer()
		{
			OriginalPrimaryPageIndex = -1;
			PrimaryOffset = -1;
			PrimaryUnknown = -1;
			OriginalSecondaryPageIndex = -1;
			SecondaryOffset = -1;
			SecondaryUnknown = -1;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ExtractedResourcePointer" /> class.
		/// </summary>
		/// <param name="basePointer">The original location information for the resource.</param>
		public ExtractedResourcePointer(ResourcePointer basePointer)
		{
			OriginalPrimaryPageIndex = (basePointer.PrimaryPage != null) ? basePointer.PrimaryPage.Index : -1;
			PrimaryOffset = basePointer.PrimaryOffset;
			PrimaryUnknown = basePointer.PrimaryUnknown;
			OriginalSecondaryPageIndex = (basePointer.SecondaryPage != null) ? basePointer.SecondaryPage.Index : -1;
			SecondaryOffset = basePointer.SecondaryOffset;
			SecondaryUnknown = basePointer.SecondaryUnknown;
		}

		/// <summary>
		///     Gets or sets the original index of the resource's primary page.
		/// </summary>
		public int OriginalPrimaryPageIndex { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource in its primary page.
		/// </summary>
		public int PrimaryOffset { get; set; }

		public int PrimaryUnknown { get; set; }

		/// <summary>
		///     Gets or sets the original index of the resource's secondary page.
		/// </summary>
		public int OriginalSecondaryPageIndex { get; set; }

		/// <summary>
		///     Gets or sets the offset of the resource in its secondary page.
		/// </summary>
		public int SecondaryOffset { get; set; }

		public int SecondaryUnknown { get; set; }
	}

	/// <summary>
	///     Contains information about a resource link extracted from a cache file.
	/// </summary>
	public class ExtractedResourceInfo
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ExtractedResourceInfo" /> class.
		/// </summary>
		/// <param name="baseResource">The <see cref="Resource" /> to initialize the instance with.</param>
		public ExtractedResourceInfo(Resource baseResource)
		{
			OriginalIndex = baseResource.Index;
			Flags = baseResource.Flags;
			Type = baseResource.Type;
			Info = baseResource.Info;
			OriginalParentTagIndex = (baseResource.ParentTag != null) ? baseResource.ParentTag.Index : DatumIndex.Null;
			Location = (baseResource.Location != null) ? new ExtractedResourcePointer(baseResource.Location) : null;
			ResourceFixups = new List<ResourceFixup>(baseResource.ResourceFixups);
			DefinitionFixups = new List<ResourceDefinitionFixup>(baseResource.DefinitionFixups);
			Unknown1 = baseResource.Unknown1;
			Unknown2 = baseResource.Unknown2;
			Unknown3 = baseResource.Unknown3;
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="ExtractedResourceInfo" /> class.
		/// </summary>
		/// <param name="originalIndex">The original datum index of the resource.</param>
		public ExtractedResourceInfo(DatumIndex originalIndex)
		{
			OriginalIndex = originalIndex;
			ResourceFixups = new List<ResourceFixup>();
			DefinitionFixups = new List<ResourceDefinitionFixup>();
		}

		/// <summary>
		///     Gets the original datum index of the resource.
		/// </summary>
		public DatumIndex OriginalIndex { get; private set; }

		/// <summary>
		///     Gets or sets flags for the resource.
		/// </summary>
		public uint Flags { get; set; }

		/// <summary>
		///     Gets or sets the name of the resource's type.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		///     Gets or sets the info buffer for the resource.
		/// </summary>
		public byte[] Info { get; set; }

		/// <summary>
		///     Gets or sets the original datum index of the tag associated with the resource.
		/// </summary>
		public DatumIndex OriginalParentTagIndex { get; set; }

		/// <summary>
		///     Gets or sets information about the resource's original location.
		/// </summary>
		public ExtractedResourcePointer Location { get; set; }

		public List<ResourceFixup> ResourceFixups { get; private set; }
		public List<ResourceDefinitionFixup> DefinitionFixups { get; private set; }

		public int Unknown1 { get; set; }
		public int Unknown2 { get; set; }
		public int Unknown3 { get; set; }
	}
}