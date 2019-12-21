﻿using System.Globalization;

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

		public RawData(string name, uint offset, long address, string value, int length, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_value = value;
			_length = length;
		}

		public RawData(string name, uint offset, string format, long address, string value, int length, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_value = value;
			_length = length;
			_format = format;
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
				DataAddress = uint.Parse(value, NumberStyles.HexNumber);
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

		public int MaxLength
		{
			get { return _format == "bytes" ? _length * 2 : _length; }
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitRawData(this);
		}

		public override MetaField CloneValue()
		{
			return new RawData(Name, Offset, FieldAddress, _value, _length, PluginLine);
		}
	}
}