using System;
using System.Windows;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	/// <summary>
	///     Abstract base class for meta fields that have values associated with them.
	/// </summary>
	public abstract class ValueField : MetaField
	{
		private long _address;
		private string _name;
		private uint _offset;
		private string _tooltip;

		public ValueField(string name, uint offset, long address, uint pluginLine, string tooltip)
		{
			_name = name;
			_offset = offset;
			_address = address;
			PluginLine = pluginLine;
			_tooltip = tooltip;
		}

		/// <summary>
		///     The value's name.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyPropertyChanged("Name");
			}
		}

		/// <summary>
		///     The offset, from the start of the current meta block or tag block, of the field's value.
		/// </summary>
		public uint Offset
		{
			get { return _offset; }
			set
			{
				_offset = value;
				NotifyPropertyChanged("Offset");
			}
		}

		/// <summary>
		///     The estimated memory address of the field itself.
		///     Do not rely upon the accuracy of this value, especially when saving or poking.
		/// </summary>
		public long FieldAddress
		{
			get { return _address; }
			set
			{
				_address = value;
				NotifyPropertyChanged("FieldAddress");
			}
		}

		/// <summary>
		///     The value's tooltip.
		/// </summary>
		public string ToolTip
		{
			get
			{
				return _tooltip;
			}
			set
			{
				_tooltip = value;
				NotifyPropertyChanged("ToolTip");
			}
		}

		/// <summary>
		/// Used when dumping to json, needs to return a type that JSON supports
		/// </summary>
		/// <returns></returns>
		public virtual object GetAsJson()
		{
			return this.AsString();
		}

		public bool ToolTipExists
		{
			get { return !string.IsNullOrEmpty(_tooltip); }
		}

		internal static float ToRadian(float input)
		{
			return (float)(input * (Math.PI / 180));
		}

		internal static float FromRadian(float input)
		{
			return (float)(input * (180 / Math.PI));
		}
	}
}