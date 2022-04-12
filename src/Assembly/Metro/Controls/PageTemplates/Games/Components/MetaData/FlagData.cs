using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData
{
	public enum FlagsType
	{
		Flags8,
		Flags16,
		Flags32,
		Flags64
	}

	public class FlagData : ValueField
	{
		private readonly SortedList<int, BitData> _bits = new SortedList<int, BitData>();
		private FlagsType _type;
		private ulong _value;

		public FlagData(string name, uint offset, long address, FlagsType type, uint pluginLine, string tooltip)
			: base(name, offset, address, pluginLine, tooltip)
		{
			_type = type;
			CheckAllCommand = new QuickCheckCommand(this, true);
			UncheckAllCommand = new QuickCheckCommand(this, false);
		}

		public ulong Value
		{
			get { return _value; }
			set
			{
				_value = value;
				NotifyPropertyChanged("Value");
				RefreshBits();
			}
		}

		public FlagsType Type
		{
			get { return _type; }
			set
			{
				_type = value;
				NotifyPropertyChanged("Type");
				NotifyPropertyChanged("TypeStr");
			}
		}

		public string TypeStr
		{
			get { return _type.ToString().ToLower(); }
		}

		public IEnumerable<BitData> Bits
		{
			get { return _bits.Values; }
		}

		public ICommand CheckAllCommand { get; private set; }

		public ICommand UncheckAllCommand { get; private set; }

		public void DefineBit(int index, string name, string tooltip)
		{
			var data = new BitData(this, name, index, tooltip);
			_bits[index] = data;
		}

		public override void Accept(IMetaFieldVisitor visitor)
		{
			visitor.VisitFlags(this);
		}

		public override MetaField CloneValue()
		{
			var result = new FlagData(Name, Offset, FieldAddress, _type, PluginLine, ToolTip);
			foreach (var bit in _bits)
				result.DefineBit(bit.Key, bit.Value.Name, bit.Value.ToolTip);
			result.Value = _value;
			return result;
		}

		public override string AsString()
		{
			return string.Format("{0} | {1} | {2}", TypeStr, Name, Value);
		}

		private void RefreshBits()
		{
			foreach (BitData bit in Bits)
				bit.Refresh();
		}

		/// <summary>
		///     Command that quickly checks or unchecks all bits in a flags field.
		/// </summary>
		private class QuickCheckCommand : ICommand
		{
			private readonly bool _check;
			private readonly FlagData _data;

			/// <summary>
			///     Initializes a new instance of the <see cref="QuickCheckCommand" /> class.
			/// </summary>
			/// <param name="data">The flags field.</param>
			/// <param name="check">
			///     If set to <c>true</c>, the command will check all bits in the field, otherwise it will uncheck all
			///     bits in the field.
			/// </param>
			public QuickCheckCommand(FlagData data, bool check)
			{
				_data = data;
				_check = check;
			}

			/// <summary>
			///     Defines the method that determines whether the command can execute in its current state.
			/// </summary>
			/// <param name="parameter">
			///     Data used by the command.  If the command does not require data to be passed, this object can
			///     be set to null.
			/// </param>
			/// <returns>
			///     true if this command can be executed; otherwise, false.
			/// </returns>
			public bool CanExecute(object parameter)
			{
				// Just make sure the flags field has bits in it
				return _data.Bits.Any();
			}

			/// <summary>
			///     Occurs when changes occur that affect whether or not the command should execute.
			/// </summary>
			public event EventHandler CanExecuteChanged
			{
				add { }
				remove { }
			}

			/// <summary>
			///     Defines the method to be called when the command is invoked.
			/// </summary>
			/// <param name="parameter">
			///     Data used by the command.  If the command does not require data to be passed, this object can
			///     be set to null.
			/// </param>
			public void Execute(object parameter)
			{
				foreach (BitData bit in _data.Bits)
					bit.IsSet = _check;
			}
		}
	}

	public class BitData : PropertyChangeNotifier
	{
		private readonly ulong _mask;
		private readonly FlagData _parent;
		private string _name;
		private string _tooltip;
		private int _index;

		public BitData(FlagData parent, string name, int index, string tooltip)
		{
			_parent = parent;
			_name = name;
			_index = index;
			_mask = (ulong)1U << index;
			_tooltip = tooltip;
		}

		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				NotifyPropertyChanged("Name");
			}
		}

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

		public bool ToolTipExists
		{
			get { return !string.IsNullOrEmpty(_tooltip); }
		}

		public bool IsSet
		{
			get { return (_parent.Value & _mask) > 0; }
			set
			{
				if (value)
					_parent.Value |= _mask;
				else
					_parent.Value &= ~_mask;

				// Changing the parent value causes a refresh,
				// so no need to call NotifyPropertyChanged
			}
		}

		public int Index
		{
			get { return _index; }
			set
			{
				_index = value;
				NotifyPropertyChanged("Index");
			}
		}

		public void Refresh()
		{
			NotifyPropertyChanged("IsSet");
		}
	}
}