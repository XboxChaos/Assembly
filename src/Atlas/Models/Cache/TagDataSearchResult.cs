using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Models.Cache
{
	public class TagDataSearchResult
	{
		/// <summary>
		///     Constructs a new search result holder.
		/// </summary>
		/// <param name="foundField">The field that was found and highlighted.</param>
		/// <param name="listField">
		///     The top-level field in the field list. For reflexive entries, this is the topmost wrapper
		///     field, otherwise, this may be the same as foundField.
		/// </param>
		/// <param name="parent">The tag data that the field is in. Can be null.</param>
		public TagDataSearchResult(TagDataField foundField, TagDataField listField, TagBlockData parent)
		{
			ListField = listField;
			Field = foundField;
			TagBlock = parent;
		}

		public TagDataField Field { get; private set; }
		public TagDataField ListField { get; private set; }
		public TagBlockData TagBlock { get; private set; }
	}
}
