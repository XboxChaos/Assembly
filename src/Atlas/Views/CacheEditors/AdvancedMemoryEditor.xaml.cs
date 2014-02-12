using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Atlas.Models;
using Atlas.ViewModels;

namespace Atlas.Views.CacheEditors
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
			ValidateByteCount();
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
			if (MemoryByteCountTextBox.Text.Length != 0)
			{
				Int32 tmp32;
				if (Int32.TryParse(MemoryByteCountTextBox.Text, out tmp32))
					MemoryByteCountTextBox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
				else
					MemoryByteCountTextBox.BorderBrush = (Brush)FindResource("AssemblyAccentBrush");
			}
			if (MemoryByteCountTextBox.Text.Length == 0 && (MemoryTypeComboBox.SelectedIndex == 10 || MemoryTypeComboBox.SelectedIndex == 11))
				MemoryByteCountTextBox.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#FF595959");
			if (MemoryByteCountTextBox.Text.Length == 0 && MemoryTypeComboBox.SelectedIndex == 12)
				MemoryByteCountTextBox.BorderBrush = (Brush)FindResource("AssemblyAccentBrush");
		}

		private void CheckDataForType()
		{
			int byteCount = -1;
			if (!Equals(MemoryByteCountTextBox.BorderBrush, FindResource("AssemblyAccentBrush")) && MemoryByteCountTextBox.Text.Length != 0)
				byteCount = Int32.Parse(MemoryByteCountTextBox.Text);

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
			uint offset;
			byte[] data;

			if (MemoryOffsetTextBox.Text.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
			{
				string hex = MemoryOffsetTextBox.Text.Substring(2);
				offset = UInt32.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture);
			}
			else
				offset = UInt32.Parse(MemoryOffsetTextBox.Text);

			switch (MemoryTypeComboBox.SelectedIndex)
			{
				case 0:
					MemoryDataTextBox.Text = ((sbyte)ViewModel.PeekByte(offset)).ToString();
					break;
				case 1:
					MemoryDataTextBox.Text = ViewModel.PeekByte(offset).ToString();
					break;
				case 2:
					MemoryDataTextBox.Text = BitConverter.ToInt16(PeekBytes(offset, 2, true), 0).ToString();
					break;
				case 3:
					MemoryDataTextBox.Text = BitConverter.ToUInt16(PeekBytes(offset, 2, true), 0).ToString();
					break;
				case 4:
					MemoryDataTextBox.Text = BitConverter.ToInt32(PeekBytes(offset, 4, true), 0).ToString();
					break;
				case 5:
					MemoryDataTextBox.Text = BitConverter.ToUInt32(PeekBytes(offset, 4, true), 0).ToString();
					break;
				case 6:
					MemoryDataTextBox.Text = BitConverter.ToInt64(PeekBytes(offset, 8, true), 0).ToString();
					break;
				case 7:
					MemoryDataTextBox.Text = BitConverter.ToUInt64(PeekBytes(offset, 8, true), 0).ToString();
					break;
				case 8:
					MemoryDataTextBox.Text = BitConverter.ToSingle(PeekBytes(offset, 4, true), 0).ToString();
					break;
				case 9:
					MemoryDataTextBox.Text = BitConverter.ToDouble(PeekBytes(offset, 8, true), 0).ToString();
					break;
				case 10:
					if (MemoryByteCountTextBox.Text.Length != 0)
						MemoryDataTextBox.Text = Encoding.ASCII.GetString(PeekBytes(offset, Int32.Parse(MemoryByteCountTextBox.Text), false));
					else
					{
						// Guesses string length, is there any way to speed this up?
						List<byte> bytesList = new List<byte>();
						for (uint i = 0; ; i++)
						{
							byte byteI = ViewModel.PeekByte(offset + i);
							if (byteI == 0)
								break;
							else
								bytesList.Add(byteI);
						}
						byte[] byteArray = bytesList.ToArray();
						MemoryDataTextBox.Text = Encoding.ASCII.GetString(byteArray);
						MemoryByteCountTextBox.Text = byteArray.Length.ToString();
					}
					break;
				case 11:
					offset += 1;
					if (MemoryByteCountTextBox.Text.Length != 0)
						MemoryDataTextBox.Text = Encoding.Unicode.GetString(PeekBytes(offset, Int32.Parse(MemoryByteCountTextBox.Text) * 2, false));
					else
					{
						// Guesses string length, is there any way to speed this up?
						List<byte> bytesList = new List<byte>();
						for (uint i = 0; ; i++)
						{
							byte byteI = ViewModel.PeekByte(offset + i);
							byte byteIplus1 = ViewModel.PeekByte(offset + i + 1);
							if (byteI == 0 && byteIplus1 == 0)
							{
								bytesList.Add(byteIplus1);
								break;
							}
							else
								bytesList.Add(byteI);
						}
						byte[] byteArray = bytesList.ToArray();
						MemoryDataTextBox.Text = Encoding.Unicode.GetString(byteArray);
						MemoryByteCountTextBox.Text = (byteArray.Length / 2).ToString();
					}
					break;
				case 12:
						MemoryDataTextBox.Text = BitConverter.ToString(ViewModel.PeekBytes(offset, Int32.Parse(MemoryByteCountTextBox.Text))).Replace("-", "");
					break;
				default:
					data = new byte[] { 0 };
					break;
			}
		}

		private void MemoryPokeButton_Click(object sender, RoutedEventArgs e)
		{
			uint offset;
			byte[] data;

			if (MemoryOffsetTextBox.Text.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
			{
				string hex = MemoryOffsetTextBox.Text.Substring(2);
				offset = UInt32.Parse(hex, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture);
			}
			else
				offset = UInt32.Parse(MemoryOffsetTextBox.Text);

			switch (MemoryTypeComboBox.SelectedIndex)
			{
				case 0:
					data = new byte[] { (byte)SByte.Parse(MemoryDataTextBox.Text) };
					break;
				case 1:
					data = new byte[] { Byte.Parse(MemoryDataTextBox.Text) };
					break;
				case 2:
					data = BitConverter.GetBytes(Int16.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 3:
					data = BitConverter.GetBytes(UInt16.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 4:
					data = BitConverter.GetBytes(Int32.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 5:
					data = BitConverter.GetBytes(UInt32.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 6:
					data = BitConverter.GetBytes(Int64.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 7:
					data = BitConverter.GetBytes(UInt64.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 8:
					data = BitConverter.GetBytes(Single.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 9:
					data = BitConverter.GetBytes(Double.Parse(MemoryDataTextBox.Text));
					Array.Reverse(data);
					break;
				case 10:
					data = Encoding.ASCII.GetBytes(MemoryDataTextBox.Text);
					break;
				case 11:
					data = Encoding.Unicode.GetBytes(MemoryDataTextBox.Text);
					offset += 1;
					break;
				case 12:
					var hex = MemoryDataTextBox.Text;

					if (hex.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
						hex = hex.Substring(2);

					char[] chars = hex.ToCharArray();
					List<byte> byteList = new List<byte>();
					for (int i = 0; i < hex.Length; i += 2)
					{
						List<char> tmpChars = new List<char>();
						tmpChars.Add(chars[i]);
						tmpChars.Add(chars[i + 1]);
						char[] tmpCharsAsArray = tmpChars.ToArray();
						string charsAsString = new string(tmpCharsAsArray);
						byte tmpByte = Byte.Parse(charsAsString, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture);
						byteList.Add(tmpByte);
					}
					data = byteList.ToArray();
					break;
				default:
					data = new byte[] { 0 };
					break;
			}

			ViewModel.PokeBytes(offset, data);
		}

		private byte[] PeekBytes(uint offset, int byteCount, bool reverseArray)
		{
			byte[] data;

			data = ViewModel.PeekBytes(offset, byteCount);
			if (reverseArray == true)
				Array.Reverse(data);

			return data;
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
