using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows;
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

			MemoryOffsetTextbox.Text = "0x" + value.Address.ToString("X8");
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
					break;
				case "utf":
				case "unicode":
					MemoryTypeComboBox.SelectedIndex = 11;
					break;
				case "bytes":
					MemoryTypeComboBox.SelectedIndex = 12;
					break;
					
				default:
					throw new InvalidDataException();
			}
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
					MemoryByteCountTextBox.Text = MemoryDataTextBox.Text.Length.ToString();
					MemoryByteCountTextBox.IsReadOnly = false;
					break;
				case 12:
					MemoryByteCountTextBox.Text = (MemoryDataTextBox.Text.Length / 2).ToString();
					MemoryByteCountTextBox.IsReadOnly = false;
					break;
			}
		}

		private void MemoryDataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			switch (MemoryTypeComboBox.SelectedIndex)
			{
				case 10:
				case 11:
					MemoryByteCountTextBox.Text = MemoryDataTextBox.Text.Length.ToString();
					MemoryByteCountTextBox.IsReadOnly = false;
					break;
				case 12:
					MemoryByteCountTextBox.Text = (MemoryDataTextBox.Text.Length / 2).ToString();
					MemoryByteCountTextBox.IsReadOnly = false;
					break;
			}
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
