using Blamite.IO;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public class DataRef : RawData
	{
		public DataRef(string name, uint offset, string format, long address, long dataAddress, string value, int length,
			uint pluginLine, FileSegmentGroup metaArea)
			: base(name, offset, format, address, value, length, pluginLine, metaArea)
		{
			DataAddress = dataAddress;
			Format = format;
			_metaArea = metaArea;
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
			var result = new DataRef(Name, Offset, Format, FieldAddress, DataAddress, Value, Length, PluginLine, _metaArea);
			return result;
		}
	}
}