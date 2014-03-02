using System.Collections.Generic;
using Atlas.Helpers;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Atlas.Models.Cache;
using Blamite.Blam;
using Blamite.Blam.Resources;
using Blamite.Flexibility;
using Blamite.Injection;
using Blamite.IO;
using Blamite.Plugins;
using Blamite.Util;
using System.Xml;

namespace Atlas.ViewModels.Cache.Dialog
{
	public class TagExtractorViewModel : Base
	{
		public TagExtractorViewModel(ICacheFile cacheFile, EngineDescription engineDescription, IStreamManager streamManager, TagHierarchyNode tag)
		{
			CacheFile = cacheFile;
			EngineDescription = engineDescription;
			StreamManager = streamManager;
			Tag = tag;

			// Setup everything
			TagContainer = new TagContainer();
			TagsToProcess = new Queue<ITag>();
			TagsProcessed = new HashSet<ITag>();
			ResourcesToProcess = new Queue<DatumIndex>();
			ResourcesProcessed = new HashSet<DatumIndex>();
			ResourcePagesProcessed = new HashSet<ResourcePage>();

			RelevantTags = new List<ExtractingTagEntry>();
			RelevantResources = new List<ExtractingResourceEntry>();

			// Check what we're going to extract
			ProcessTags();
		}

		private void ProcessTags()
		{
			// Recursively process tags
			TagsToProcess.Enqueue(Tag.Tag);

			ResourceTable resources = null;
			using (var reader = StreamManager.OpenRead())
			{
				while (TagsToProcess.Count > 0)
				{
					var currentTag = TagsToProcess.Dequeue();
					if (TagsProcessed.Contains(currentTag))
						continue;

					// Get the plugin path
					var className = VariousFunctions.SterilizeTagClassName(CharConstant.ToString(currentTag.Class.Magic)).Trim();
					var pluginPath = VariousFunctions.GetPluginPath(EngineDescription.Settings.GetSetting<string>("plugins"), className);

					var blockBuilder = new DataBlockBuilder(reader, currentTag.MetaLocation, CacheFile, EngineDescription);
					using (var pluginReader = XmlReader.Create(pluginPath))
						AssemblyPluginLoader.LoadPlugin(pluginReader, blockBuilder);

					// Add data for the tag that was processed
					var tagName = CacheFile.FileNames.GetTagName(currentTag) ?? currentTag.Index.ToString();
					var extractedTag = new ExtractedTag(currentTag.Index, currentTag.MetaLocation.AsPointer(), currentTag.Class.Magic,
						tagName);
					RelevantTags.Add(new ExtractingTagEntry(true, extractedTag));

					// Mark the tag as processed and then enqueue all of its child tags and resources
					TagsProcessed.Add(currentTag);
					foreach (var tagRef in blockBuilder.ReferencedTags)
						TagsToProcess.Enqueue(CacheFile.Tags[tagRef]);
					foreach (var resource in blockBuilder.ReferencedResources)
						ResourcesToProcess.Enqueue(resource);
				}

				// Load the resource table in if necessary
				if (ResourcesToProcess.Count > 0)
					resources = CacheFile.Resources.LoadResourceTable(reader);
			}

			// Extract resource info
			if (resources == null)
				return;

			#region Resources

			while (ResourcesToProcess.Count > 0)
			{
				var index = ResourcesToProcess.Dequeue();
				if (ResourcesProcessed.Contains(index))
					continue;

				// Add the resource
				var resource = resources.Resources[index.Index];
				RelevantResources.Add(new ExtractingResourceEntry(true, resource, new ExtractedResourceInfo(resource)));
			}

			#endregion
		}

		#region Properties

		#region Cache Data

		public ICacheFile CacheFile
		{
			get { return _cacheFile; }
			set { SetField(ref _cacheFile, value); }
		}
		private ICacheFile _cacheFile;

		public EngineDescription EngineDescription
		{
			get { return _engineDescription; }
			set { SetField(ref _engineDescription, value); }
		}
		private EngineDescription _engineDescription;

		public IStreamManager StreamManager
		{
			get { return _streamManager; }
			set { SetField(ref _streamManager, value); }
		}
		private IStreamManager _streamManager;

		public TagHierarchyNode Tag
		{
			get { return _tag; }
			set { SetField(ref _tag, value); }
		}
		private TagHierarchyNode _tag;

		#endregion

		public TagContainer TagContainer
		{
			get { return _tagContainer; }
			set { SetField(ref _tagContainer, value); }
		}
		private TagContainer _tagContainer;

		public Queue<ITag> TagsToProcess
		{
			get { return _tagsToProcess; }
			set { SetField(ref _tagsToProcess, value); }
		}
		private Queue<ITag> _tagsToProcess;

		public HashSet<ITag> TagsProcessed
		{
			get { return _tagsProcessed; }
			set { SetField(ref _tagsProcessed, value); }
		}
		private HashSet<ITag> _tagsProcessed;

		public Queue<DatumIndex> ResourcesToProcess
		{
			get { return _resourcesToProcess; }
			set { SetField(ref _resourcesToProcess, value); }
		}
		private Queue<DatumIndex> _resourcesToProcess;

		public HashSet<DatumIndex> ResourcesProcessed
		{
			get { return _resourcesProcessed; }
			set { SetField(ref _resourcesProcessed, value); }
		}
		private HashSet<DatumIndex> _resourcesProcessed;

		public HashSet<ResourcePage> ResourcePagesProcessed
		{
			get { return _resourcePagesProcessed; }
			set { SetField(ref _resourcePagesProcessed, value); }
		}
		private HashSet<ResourcePage> _resourcePagesProcessed;

		public List<ExtractingTagEntry> RelevantTags
		{
			get { return _relevantTags; }
			set { SetField(ref _relevantTags, value); }
		}
		private List<ExtractingTagEntry> _relevantTags;

		public ExtractingTagEntry SelectedRelevantTag
		{
			get { return _selectedRelevantTag; }
			set { SetField(ref _selectedRelevantTag, value); }
		}
		private ExtractingTagEntry _selectedRelevantTag;

		public List<ExtractingResourceEntry> RelevantResources
		{
			get { return _relevantResources; }
			set { SetField(ref _relevantResources, value); }
		}
		private List<ExtractingResourceEntry> _relevantResources;

		#endregion
	}
}
