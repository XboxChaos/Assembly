using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Atlas.Models;

namespace Atlas.Views.Cache.TagEditorComponents.Data
{
	public enum BitfieldType
	{
		Bitfield8,
		Bitfield16,
		Bitfield32
	}

	public class BitfieldData : ValueField
	{
		private readonly SortedList<int, BitData> _bits = new SortedList<int, BitData>();
		private BitfieldType _type;
		private uint _value;

		public BitfieldData(string name, uint offset, uint address, BitfieldType type, uint pluginLine)
			: base(name, offset, address, pluginLine)
		{
			_type = type;
			CheckAllCommand = new QuickCheckCommand(this, true);
			UncheckAllCommand = new QuickCheckCommand(this, false);
		}

		public uint Value
		{
			get { return _value; }
			set
			{
				_value = value;
				OnPropertyChanged("Value");
				RefreshBits();
			}
		}

		public BitfieldType Type
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

		public IEnumerable<BitData> Bits
		{
			get { return _bits.Values; }
		}

		public ICommand CheckAllCommand { get; private set; }

		public ICommand UncheckAllCommand { get; private set; }

		public void DefineBit(int index, string name)
		{
			var data = new BitData(this, name, index);
			_bits[index] = data;
		}

		public override void Accept(ITagDataFieldVisitor visitor)
		{
			visitor.VisitBitfield(this);
		}

		public override TagDataField CloneValue()
		{
			var result = new BitfieldData(Name, Offset, FieldAddress, _type, base.PluginLine);
			foreach (var bit in _bits)
				result.DefineBit(bit.Key, bit.Value.Name);
			result.Value = _value;
			return result;
		}

		private void RefreshBits()
		{
			foreach (BitData bit in Bits)
				bit.Refresh();
		}

		/// <summary>
		///     Command that quickly checks or unchecks all bits in a bitfield.
		/// </summary>
		private class QuickCheckCommand : ICommand
		{
			private readonly bool _check;
			private readonly BitfieldData _data;

			/// <summary>
			///     Initializes a new instance of the <see cref="QuickCheckCommand" /> class.
			/// </summary>
			/// <param name="data">The biefield.</param>
			/// <param name="check">
			///     If set to <c>true</c>, the command will check all bits in the field, otherwise it will uncheck all
			///     bits in the field.
			/// </param>
			public QuickCheckCommand(BitfieldData data, bool check)
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
				// Just make sure the bitfield has bits in it
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

	public class BitData : Base
	{
		private readonly uint _mask;
		private readonly BitfieldData _parent;
		private string _name;

		public BitData(BitfieldData parent, string name, int index)
		{
			_parent = parent;
			_name = name;
			_mask = 1U << index;
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
				// so no need to call OnPropertyChanged
			}
		}

		public void Refresh()
		{
			OnPropertyChanged("IsSet");
		}
	}
}