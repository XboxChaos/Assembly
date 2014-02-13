namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	/// <summary>
	///     Abstract base class for meta fields that have values associated with them.
	/// </summary>
	public abstract class ValueField : TagDataField
	{
		private uint _address;
		private string _name;
		private uint _offset;

		public ValueField(string name, uint offset, uint address, uint pluginLine)
		{
			_name = name;
			_offset = offset;
			_address = address;
			PluginLine = pluginLine;
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
				OnPropertyChanged("Name");
			}
		}

		/// <summary>
		///     The offset, from the start of the current meta block or reflexive, of the field's value.
		/// </summary>
		public uint Offset
		{
			get { return _offset; }
			set
			{
				_offset = value;
				OnPropertyChanged("Offset");
			}
		}

		/// <summary>
		///     The estimated memory address of the field itself.
		///     Do not rely upon the accuracy of this value, especially when saving or poking.
		/// </summary>
		public uint FieldAddress
		{
			get { return _address; }
			set
			{
				_address = value;
				OnPropertyChanged("FieldAddress");
			}
		}
	}
}