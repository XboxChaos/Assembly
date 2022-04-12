using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Blamite.Serialization;
using System.IO;
using Assembly.Helpers.Native;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for EnginePicker.xaml
	/// </summary>
	public partial class EnginePicker
	{
		private List<EngineDescription> Engines;
		public EnginePicker(string path, List<EngineDescription> engines)
		{
			Engines = engines;
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
			txtMapName.Text = Path.GetFileName(path);
			listbox.ItemsSource = Engines;

			Activate();
		}

		public EngineDescription Selection { get; private set; }

		private void BtnContinue_OnClick(object sender, RoutedEventArgs e)
		{
			if (listbox.SelectedItem != null)
				Selection = (EngineDescription)listbox.SelectedItem;
			else
				Selection = null;

			Close();
		}

		private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Selection = null;
			Close();
		}

		private void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			btnContinue.IsEnabled = listbox.SelectedItems.Count > 0;
		}

		private void TextBlock_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ClickCount >= 2)
			{
				Selection = (EngineDescription)listbox.SelectedItem;
				Close();
			}
		}
	}
}