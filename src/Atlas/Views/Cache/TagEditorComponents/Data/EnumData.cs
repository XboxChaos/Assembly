using System.Collections.ObjectModel;
using Atlas.Models;
using Atlas.Views.Cache.TagEditorComponents.Data;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public enum EnumType
	{
		Enum8,
		Enum16,
		Enum32
	}

	public class EnumData : ValueField
	{
		private EnumValue _selectedValue;
		private EnumType _type;
		private int _value;

		public EnumData(string name, uint offset, uint address, EnumType type, int value, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_type = type;
			_value = value;
			Values = new ObservableCollection<EnumValue>();
		}

		public EnumType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				OnPropertyChanged("Type");
				OnPropertyChanged("TypeStr");
			}
		}

		public string TypeStr
		{
			get { return _type.ToString().ToLower(); }
		}

		public int Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		public EnumValue SelectedValue
		{
			get { return _selectedValue; }
			set
			{
				_selectedValue = value;
				OnPropertyChanged("SelectedValue");
				Value = value.Value;
			}
		}

		public ObservableCollection<EnumValue> Values { get; private set; }

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitEnum(this);
		}

		public override TagDataField CloneValue()
		{
			var result = new EnumData(Name, Offset, FieldAddress, _type, _value, base.PluginLine);
			foreach (EnumValue option in Values)
			{
				var copiedValue = new EnumValue(option.Name, option.Value);
				result.Values.Add(copiedValue);
				if (_selectedValue != null && copiedValue.Value == _selectedValue.Value)
					result._selectedValue = copiedValue;
			}
			return result;
		}
	}

	public class EnumValue : Base
	{
		private string _name;
		private int _value;

		public EnumValue(string name, int value)
		{
			_name = name;
			_value = value;
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				OnPropertyChanged("Name");
			}
		}

		public int Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}
	}
}