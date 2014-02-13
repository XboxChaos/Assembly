using System.Collections.Generic;
using Atlas.Models;
using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.ViewModels.Dialog
{
	public class ViewValueAsViewModel : Base
	{
		public ViewValueAsViewModel(CachePageViewModel cachePageViewModel, uint cacheOffsetOriginal,
			IList<TagDataField> tagDataFieldList, string title = "Tag Data Inspector")
		{
			CachePageViewModel = cachePageViewModel;
			CacheOffset = CacheOffsetOriginal = cacheOffsetOriginal;
			TagDataFieldList = tagDataFieldList;
			Title = title;
		}

		#region Properties

		public string Title
		{
			get { return _title; }
			set { SetField(ref _title, value); }
		}
		private string _title;

		public CachePageViewModel CachePageViewModel
		{
			get { return _cachePageViewModel; }
			set { SetField(ref _cachePageViewModel, value); }
		}
		private CachePageViewModel _cachePageViewModel;

		public uint CacheOffsetOriginal
		{
			get { return _cacheOffsetOriginal; }
			set { SetField(ref _cacheOffsetOriginal, value); }
		}
		private uint _cacheOffsetOriginal;

		public uint CacheOffset
		{
			get { return _cacheOffset; }
			set { SetField(ref _cacheOffset, value); }
		}
		private uint _cacheOffset;

		public TagDataReader TagDataReader
		{
			get { return _tagDataReader; }
			set { SetField(ref _tagDataReader, value); }
		}
		private TagDataReader _tagDataReader;

		public IList<TagDataField> TagDataFieldList
		{
			get { return _tagDataFieldList; }
			set { SetField(ref _tagDataFieldList, value); }
		}
		private IList<TagDataField> _tagDataFieldList;

		#endregion

		public void RefreshTagData()
		{
			TagDataReader = new TagDataReader(CachePageViewModel.MapStreamManager, _cacheOffset, CachePageViewModel.CacheFile,
				CachePageViewModel.EngineDescription, TagDataReader.LoadType.File, null);
			TagDataReader.ReadFields(TagDataFieldList);
		}
	}
}
