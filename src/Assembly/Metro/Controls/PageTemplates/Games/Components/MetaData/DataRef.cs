using Assembly.Helpers;
using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class DataRef : RawData
	{
		public DataRef(string name, uint offset, string format, long address, long dataAddress, string value, int length,
			uint pluginLine, string tooltip, FileSegmentGroup metaArea, TagDataCommandState tagCommandState)
			: base(name, offset, format, address, value, length, pluginLine, tooltip, metaArea, tagCommandState)
		{
			DataAddress = dataAddress;
			Format = format;
			_metaArea = metaArea;
			_tagCommandState = tagCommandState;
		}

		public new string Type
		{
			get { return "dataref"; }
		}
		
		public new string FullType
		{
			get { return Type + " " + Format; }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDataRef(this);
		}

		public override MetaField CloneValue()
		{
			var result = new DataRef(Name, Offset, Format, FieldAddress, DataAddress, Value, Length, PluginLine, ToolTip, _metaArea, _tagCommandState);
			return result;
		}
	}
}