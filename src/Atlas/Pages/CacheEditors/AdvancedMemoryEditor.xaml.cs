using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Atlas.Models;
using Atlas.ViewModels;

namespace Atlas.Pages.CacheEditors
{
	/// <summary>
	/// Interaction logic for AdvancedMemoryEditor.xaml
	/// </summary>
	public partial class AdvancedMemoryEditor : ICacheEditor
	{
		public CachePageViewModel ViewModel { get; private set; }

		public AdvancedMemoryEditor(CachePageViewModel cachePageViewModel)
		{
			InitializeComponent();
			DataContext = ViewModel = cachePageViewModel;
			EditorTitle = "Advanced Memory Editor";
		}

		public bool IsSingleInstance { get { return true; } }

		public string EditorTitle
		{
			get { return _editorTitle; }
			private set { SetField(ref _editorTitle, value); }
		}
		private string _editorTitle;

		public bool Close()
		{
			// lets leave this hear until the editor is finished. It'll remind us to add this feature when it's done, if it's needed
			throw new NotImplementedException();
		}

		private void LoadDataButton_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			if (button == null) return;

			var value = button.DataContext as EngineMemory.EngineVersion.MemoryValue;

			MemoryOffsetTextBox.Text = "0x" + value.Address.ToString("X8");
			MemoryDataTextBox.Text = value.Data;

			switch (value.Type)
			{
				case "sbyte":
				case "int8":
					MemoryTypeComboBox.SelectedIndex = 0;
					break;
				case "byte":
				case "uint8":
					MemoryTypeComboBox.SelectedIndex = 1;
					break;
				case "short":
				case "int16":
					MemoryTypeComboBox.SelectedIndex = 2;
					break;
				case "ushort":
				case "uint16":
					MemoryTypeComboBox.SelectedIndex = 3;
					break;
				case "int":
				case "int32":
					MemoryTypeComboBox.SelectedIndex = 4;
					break;
				case "uint":
				case "uint32":
					MemoryTypeComboBox.SelectedIndex = 5;
					break;
				case "long":
				case "int64":
					MemoryTypeComboBox.SelectedIndex = 6;
					break;
				case "ulong":
				case "uint64":
					MemoryTypeComboBox.SelectedIndex = 7;
					break;
				case "float":
					MemoryTypeComboBox.SelectedIndex = 8;
					break;
				case "double":
					MemoryTypeComboBox.SelectedIndex = 9;
					break;
				case "ascii":
				case "string":
					MemoryTypeComboBox.SelectedIndex = 10;
					MemoryByteCountTextBox.Text = value.Data.Length.ToString();
					break;
				case "utf":
				case "unicode":
					MemoryTypeComboBox.SelectedIndex = 11;
					MemoryByteCountTextBox.Text = value.Data.Length.ToString();
					break;
				case "bytes":
					MemoryTypeComboBox.SelectedIndex = 12;
					MemoryByteCountTextBox.Text = (value.Data.Length / 2).ToString();
					break;
					
				default:
					throw new InvalidDataException();
			}

			ValidateOffset();
			ValidateByteCount();
			CheckDataForType();
		}

		private void MemoryOffsetTextbox_TextChanged(object sender, TextChangedEventArgs e)
		{
			ValidateOffset();
			CanWePeek();
			CanWePoke();
		}

		private void MemoryTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			switch (MemoryTypeComboBox.SelectedIndex)
			{
				case 0:
				case 1:
					MemoryByteCountTextBox.Text = "1";
					MemoryByteCountTextBox.IsReadOnly = true;
					break;
				case 2:
				case 3:
					MemoryByteCountTextBox.Text = "2";
					MemoryByteCountTextBox.IsReadOnly = true;
					break;
				case 4:
				case 5:
				case 8:
					MemoryByteCountTextBox.Text = "4";
					MemoryByteCountTextBox.IsReadOnly = true;
					break;
				case 6:
				case 7:
				case 9:
					MemoryByteCountTextBox.Text = "8";
					MemoryByteCountTextBox.IsReadOnly = true;
					break;
				case 10:
				case 11:
				case 12:
					MemoryByteCountTextBox.IsReadOnly = false;
					break;
			}

			CheckDataForType();
			CanWePeek();
			CanWePoke();
		}

		private void MemoryByteCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			ValidateByteCount();
			CheckDataForType();
			CanWePeek();
			CanWePoke();
		}

		private void MemoryDataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			ValidateByteCount();
			CheckDataForType();
			CanWePoke();
		}

		private void ValidateOffset()
		{
			UInt32 tmpU32;
			bool successfulParse;

			var offset = MemoryOffsetTextBox.Text;
			if (offset.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
			{
				offset = offset.Substring(2);
				successfulParse = UInt32.TryParse(offset, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out tmpU32);
			}
			else
				successfulParse = UInt32.TryParse(offset, out tmpU32);

			if (successfulParse == true)
				MemoryOffsetTextBox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				MemoryOffsetTextBox.BorderBrush = (Brush)FindResource("AssemblyAccentBrush");
		}

		private void ValidateByteCount()
		{
			// Should we keep a limit for the byte/character count?
			UInt16 tmpU16;
			if (UInt16.TryParse(MemoryByteCountTextBox.Text, out tmpU16))
				MemoryByteCountTextBox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			else
				MemoryByteCountTextBox.BorderBrush = (Brush)FindResource("AssemblyAccentBrush");
		}

		private void CheckDataForType()
		{
			int byteCount = -1;
			if (!Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) && MemoryByteCountTextBox.Text.Length != 0)
				byteCount = UInt16.Parse(MemoryByteCountTextBox.Text);
			if (Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")))
				byteCount = 0;

			Brush validBorderBrush = new BrushConverter().ConvertFromString("#FF595959") as Brush;
			Brush invalidBorderBrush = FindResource("AssemblyAccentBrush") as Brush;

			Byte tmpByte;
			SByte tmpSByte;
			Int16 tmp16;
			UInt16 tmpU16;
			Int32 tmp32;
			UInt32 tmpU32;
			Int64 tmp64;
			UInt64 tmpU64;
			Single tmpFloat;
			Double tmpDouble;
			List<Byte> tmpBytes = new List<Byte>();
			switch (MemoryTypeComboBox.SelectedIndex)
			{
				case 0:
					if (SByte.TryParse(MemoryDataTextBox.Text, out tmpSByte))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 1:
					if (Byte.TryParse(MemoryDataTextBox.Text, out tmpByte))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 2:
					if (Int16.TryParse(MemoryDataTextBox.Text, out tmp16))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 3:
					if (UInt16.TryParse(MemoryDataTextBox.Text, out tmpU16))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 4:
					if (Int32.TryParse(MemoryDataTextBox.Text, out tmp32))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 5:
					if (UInt32.TryParse(MemoryDataTextBox.Text, out tmpU32))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 6:
					if (Int64.TryParse(MemoryDataTextBox.Text, out tmp64))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 7:
					if (UInt64.TryParse(MemoryDataTextBox.Text, out tmpU64))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 8:
					if (Single.TryParse(MemoryDataTextBox.Text, out tmpFloat))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 9:
					if (Double.TryParse(MemoryDataTextBox.Text, out tmpDouble))
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
				case 10:
					if (MemoryDataTextBox.Text.Length != byteCount)
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					break;
				case 11:
					if (MemoryDataTextBox.Text.Length != byteCount)
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					else
						MemoryDataTextBox.BorderBrush = validBorderBrush;
					break;
				case 12:
					var hex = MemoryDataTextBox.Text;

					if (hex.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
					{
						hex = hex.Substring(2);
					}

					if (byteCount * 2 != hex.Length)
					{
						MemoryDataTextBox.BorderBrush = invalidBorderBrush;
						return;
					}

					char[] chars = hex.ToCharArray();
					for (int i = 0; i < (byteCount * 2); i += 2)
					{
						List<char> charList = new List<char>();
						try
						{
							charList.Add(chars[i]);
							charList.Add(chars[i + 1]);
							char[] charsAsArray = charList.ToArray();
							string charsAsString = new string(charsAsArray);
							if (Byte.TryParse(charsAsString, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out tmpByte))
								tmpBytes.Add(tmpByte);
							else
							{
								MemoryDataTextBox.BorderBrush = invalidBorderBrush;
								return;
							}
						}
						catch
						{
							MemoryDataTextBox.BorderBrush = invalidBorderBrush;
							return;
						}
					}
					MemoryDataTextBox.BorderBrush = validBorderBrush;
					break;

				default:
					MemoryDataTextBox.BorderBrush = invalidBorderBrush;
					break;
			}
		}

		private void CanWePeek()
		{
			if (MemoryPeekButton != null)
			{
				if (!Equals(MemoryOffsetTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) && MemoryTypeComboBox.SelectedIndex != -1 && !Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")))
					MemoryPeekButton.IsEnabled = true;
				if (Equals(MemoryOffsetTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) || MemoryTypeComboBox.SelectedIndex == -1 || Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")))
					MemoryPeekButton.IsEnabled = false;
			}
		}

		private void CanWePoke()
		{
			if (MemoryPokeButton != null)
			{
				if (!Equals(MemoryOffsetTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) && MemoryTypeComboBox.SelectedIndex != -1 && !Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) && !Equals(MemoryDataTextBox.BorderBrush, FindResource("AssemblyAccentBrush")))
					MemoryPokeButton.IsEnabled = true;
				if (Equals(MemoryOffsetTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) || MemoryTypeComboBox.SelectedIndex == -1 || Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) || Equals(MemoryDataTextBox.BorderBrush, FindResource("AssemblyAccentBrush")))
					MemoryPokeButton.IsEnabled = false;
			}
		}

		private void MemoryPeekButton_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void MemoryPokeButton_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		#region Inpc Helpers

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		public virtual bool SetField<T>(ref T field, T value,
			[CallerMemberName] string propertyName = "")
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		#endregion
	}
}
