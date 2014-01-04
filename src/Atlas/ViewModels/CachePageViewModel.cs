using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Atlas.Helpers;
using Atlas.Helpers.Tags;
using Atlas.Models;
using Blamite.Blam;
using Blamite.Flexibility;
using Blamite.IO;
using Blamite.Util;

namespace Atlas.ViewModels
{
	public class CachePageViewModel : Base
	{
		public string CacheLocation
		{
			get { return _cacheLocation; }
			private set { SetField(ref _cacheLocation, value); }
		}
		private string _cacheLocation;

		public ICacheFile CacheFile
		{
			get { return _cacheFile; }
			private set { SetField(ref _cacheFile, value); }
		}
		private ICacheFile _cacheFile;

		public EngineDescription EngineDescription
		{
			get { return _engineDescription; }
			private set { SetField(ref _engineDescription, value); }
		}
		private EngineDescription _engineDescription;

		public IStreamManager MapStreamManager
		{
			get { return _mapStreamManager; }
			private set { SetField(ref _mapStreamManager, value); }
		}
		private IStreamManager _mapStreamManager;

		public Trie StringIdTrie
		{
			get { return _stringIdTrie; }
			private set { SetField(ref _stringIdTrie, value); }
		}
		private Trie _stringIdTrie;

		public TagHierarchy ClassHierarchy
		{
			get { return _classHierarchy; }
			private set { SetField(ref _classHierarchy, value); }
		}
		private TagHierarchy _classHierarchy;

		public TagHierarchy ActiveHierarchy
		{
			get { return _activeHierarchy; }
			private set { SetField(ref _activeHierarchy, value); }
		}
		private TagHierarchy _activeHierarchy;

		public Dictionary<string, object> CacheHeaderInformation
		{
			get { return _cacheHeaderInformation; }
			private set { SetField(ref _cacheHeaderInformation, value); }
		}
		private Dictionary<string, object> _cacheHeaderInformation;

		public void LoadCache(string cacheLocation)
		{
			CacheLocation = cacheLocation;

			using (var fileStream = File.OpenRead(CacheLocation))
			{
				var reader = new EndianReader(fileStream, Endian.BigEndian);
				CacheFile = CacheFileLoader.LoadCacheFile(reader, App.Storage.Settings.DefaultDatabase,
					out _engineDescription);

				MapStreamManager = new FileStreamManager(CacheLocation, reader.Endianness);

				StringIdTrie = new Trie();
				if (CacheFile.StringIDs != null)
					StringIdTrie.AddRange(CacheFile.StringIDs);

				LoadHeader();
				LoadTags();
			}
		}

		private void LoadHeader()
		{
			CacheHeaderInformation = new Dictionary<string, object>
			{
				{ "Game:", EngineDescription.Name },
				{ "Build:", CacheFile.BuildString.ToString(CultureInfo.InvariantCulture) }
			};
		}

		private void LoadTags()
		{
			if (CacheFile.TagClasses == null || CacheFile.Tags == null)
				return;

			_classHierarchy = new ClassTagHierarchy(_cacheFile);
			PopulateHierarchy(_classHierarchy);

			switch (App.Storage.Settings.CacheEditorTagSortMethod)
			{
				case Settings.TagSort.TagClass:
					ActiveHierarchy = ClassHierarchy;
					break;

				case Settings.TagSort.PathHierarchy:
					ActiveHierarchy = new PathTagHierarchy(CacheFile);
					PopulateHierarchy(ActiveHierarchy);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		#region Helpers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hierarchy"></param>
		private void PopulateHierarchy(TagHierarchy hierarchy)
		{
			foreach (var tag in _cacheFile.Tags.Where((t) => t != null && t.Class != null && t.MetaLocation != null))
				hierarchy.AddTag(tag, GetTagName(tag));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public string GetTagName(ITag tag)
		{
			var name = _cacheFile.FileNames.GetTagName(tag);
			return string.IsNullOrWhiteSpace(name) ? tag.Index.ToString() : name;
		}

		#endregion
	}
}
