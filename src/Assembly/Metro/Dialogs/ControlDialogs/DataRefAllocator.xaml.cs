using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Blamite.Blam;
using Blamite.Util;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for DataRefAllocator.xaml
	/// </summary>
	public partial class DataRefAllocator
	{
		private readonly DataRef _field;
		private readonly int _originalLength;

		public DataRefAllocator(DataRef field)
		{
			_field = field;
			_originalLength = field.Length;
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
			InitInformation();
		}

		public int? NewLength { get; private set; }

		private void InitInformation()
		{
			lblSubInfo.Text = string.Format(lblSubInfo.Text, _field.Name);
			lblOriginalAddress.Text = string.Format("0x{0:X8}", _field.DataAddress);
			lblOriginalLength.Text = _originalLength.ToString();
			txtNewLength.Text = _field.Length.ToString();
		}

		private void BtnContinue_OnClick(object sender, RoutedEventArgs e)
		{
			int newLength;
			if (!int.TryParse(txtNewLength.Text, out newLength))
			{
				MetroMessageBox.Show("Data Reference Allocator - Assembly", "Please enter a valid length.");
				return;
			}
			else if (newLength == 0)
			{
				MetroMessageBox.Show("Data Reference Allocator - Assembly",
				"The data reference allocator is not needed to zero any dataref fields.");
				return;
			}
			NewLength = newLength;
			Close();
		}

		private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void TxtNewLength_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			int newLength;
			if (!int.TryParse(txtNewLength.Text, out newLength))
				return;
			if (newLength < 0)
			{
				newLength = 0;
				txtNewLength.Text = "0";
			}
			btnContinue.IsEnabled = (newLength != _originalLength);
		}
	}
}