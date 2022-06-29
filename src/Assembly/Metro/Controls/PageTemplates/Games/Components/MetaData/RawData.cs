using Blamite.IO;
using Blamite.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Base class for raw data.
	/// </summary>
	public class RawData : ValueField
	{
		private long _dataAddress;
		private string _value;
		private string _format;
		private int _length;
		internal FileSegmentGroup _metaArea;

		public RawData(string name, uint offset, long address, string value, int length, uint pluginLine, string tooltip, FileSegmentGroup metaArea)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_value = value;
			_length = length;
			_metaArea = metaArea;
		}

		public RawData(string name, uint offset, string format, long address, string value, int length, uint pluginLine, string tooltip, FileSegmentGroup metaArea)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_value = value;
			_length = length;
			_format = format;
			_metaArea = metaArea;
		}

		public string Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
			}
		}

		public string Type
		{
			get { return "raw"; }
		}
		
		public string FullType
		{
			get { return Type + " " + Format; }
		}

		public string Format
		{
			get { return _format; }
			set
			{
				_format = value;
				NotifyPropertyChanged("Format");
			}
		}

		public long DataAddress
		{
			get { return _dataAddress; }
			set
			{
				if (value != 0 && !_metaArea.ContainsPointer(value))
					throw new ArgumentException("Invalid address for this cache file.");

				_dataAddress = value;
				NotifyPropertyChanged("DataAddress");
				NotifyPropertyChanged("DataAddressHex");
			}
		}

		public string DataAddressHex
		{
			get { return "0x" + DataAddress.ToString("X"); }
			set
			{
				if (value.StartsWith("0x"))
					value = value.Substring(2);
				DataAddress = long.Parse(value, NumberStyles.HexNumber);
			}
		}

		public int Length
		{
			get { return _length; }
			set
			{
				_length = value;
				NotifyPropertyChanged("Length");
				NotifyPropertyChanged("MaxLength");
			}
		}

		private bool IsBinary
		{
			get { return _format == "bytes"; }
		}
		public int MaxLength
		{
			get { return IsBinary ? _length * 2 : _length; }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRawData(this);
		}

		public override MetaField CloneValue()
		{
			return new RawData(Name, Offset, FieldAddress, _value, _length, PluginLine, ToolTip, _metaArea);
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | hash = {2}", Type, Name, Value.GetHashCode());
		}

		public override object GetAsJson()
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict["Offset"] = Offset;
			dict["FieldAddress"] = FieldAddress;
			dict["Length"] = _length;
			dict["Hash"] = Value.GetHashCode();
			if (IsBinary)
				dict["Encoded"] = Convert.ToBase64String(FunctionHelpers.HexStringToBytes(Value));
			else
				dict["Data"] = Value;

			return dict;
		}
	}
}