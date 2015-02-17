using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Assembly.Helpers;
using Assembly.Helpers.Native;
using Assembly.Metro.Controls.PageTemplates.Games.Components;
using Assembly.Metro.Controls.PageTemplates.Games.Components.MetaData;
using Blamite.Blam;

namespace Assembly.Metro.Dialogs.ControlDialogs
{
	/// <summary>
	///     Interaction logic for TagBlockReallocator.xaml
	/// </summary>
	public partial class TagBlockReallocator
	{
		private readonly ReflexiveData _block;

		public TagBlockReallocator(ReflexiveData block)
		{
			_block = block;
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
			InitBlockInformation();
		}

		public int? NewCount { get; private set; }

		private void InitBlockInformation()
		{
			lblSubInfo.Text = string.Format(lblSubInfo.Text, _block.Name);
			lblOriginalAddress.Text = string.Format("0x{0:X8}", _block.FirstEntryAddress);
			lblEntrySize.Text = string.Format("0x{0:X}", _block.EntrySize);
			txtNewCount.Text = _block.Length.ToString();
			UpdateTotalSize(_block.Length);
		}

		private void UpdateTotalSize(int count)
		{
			lblNewSize.Text = string.Format("0x{0:X}", count * _block.EntrySize);
		}

		private void BtnContinue_OnClick(object sender, RoutedEventArgs e)
		{
			int newCount;
			if (!int.TryParse(txtNewCount.Text, out newCount))
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly", "Please enter a valid entry count.");
				return;
			}
			NewCount = newCount;
			Close();
		}

		private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void TxtNewCount_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			int newCount;
			if (!int.TryParse(txtNewCount.Text, out newCount))
				return;
			if (newCount < 0)
			{
				newCount = 0;
				txtNewCount.Text = "0";
			}
			btnMinus.IsEnabled = (newCount > 0);
			UpdateTotalSize(newCount);
		}

		private void BtnPlus_OnClick(object sender, RoutedEventArgs e)
		{
			AddToCount(1);
		}

		private void BtnMinus_OnClick(object sender, RoutedEventArgs e)
		{
			AddToCount(-1);
		}

		private void AddToCount(int delta)
		{
			int newCount;
			if (!int.TryParse(txtNewCount.Text, out newCount))
				return;
			newCount = Math.Max(0, newCount + delta);
			txtNewCount.Text = newCount.ToString();
		}
	}
}