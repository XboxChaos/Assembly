using System.Collections.Generic;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     32-bit datum, split into an index and salt
	/// </summary>
	public class DatumData : ValueField
	{
		private ushort _salt;
		private ushort _index;

		public DatumData(string name, uint offset, long address, ushort salt, ushort index, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_salt = salt;
			_index = index;
		}

		public ushort Salt
		{
			get { return _salt; }
			set
			{
				_salt = value;
				NotifyPropertyChanged("Salt");
			}
		}

		public ushort Index
		{
			get { return _index; }
			set
			{
				_index = value;
				NotifyPropertyChanged("Index");
			}
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitDatum(this);
		}

		public override MetaField CloneValue()
		{
			return new DatumData(Name, Offset, FieldAddress, Salt, Index, PluginLine, ToolTip);
		}

		public override string AsString()
		{
			return string.Format("datum | {0} | {1} {2}", Name, Salt, Index);
		}

		public override object GetAsJson()
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict["Salt"] = Salt;
			dict["Index"] = Index;
			dict["FullInteger"] = Salt << 16 | Index;

			return dict;
		}
	}
}
