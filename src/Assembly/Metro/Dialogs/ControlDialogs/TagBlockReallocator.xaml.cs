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
	///     Interaction logic for TagBlockReallocator.xaml
	/// </summary>
	public partial class TagBlockReallocator
	{
		private readonly TagBlockData _block;
		private readonly int _originalCount;

		public TagBlockReallocator(TagBlockData block)
		{
			_block = block;
			_originalCount = block.Length;
			InitializeComponent();
			DwmDropShadow.DropShadowToWindow(this);
			InitBlockInformation();
		}

		public int? NewCount { get; private set; }

		private void InitBlockInformation()
		{
			lblSubInfo.Text = string.Format(lblSubInfo.Text, _block.Name);
			lblOriginalAddress.Text = string.Format("0x{0:X8}", _block.FirstElementAddress);
			lblOriginalCount.Text = _originalCount.ToString();
			lblEntrySize.Text = string.Format("0x{0:X}", _block.ElementSize);
			txtNewCount.Text = _block.Length.ToString();
			UpdateTotalSize(_block.Length);
		}

		private void UpdateTotalSize(int count)
		{
			lblNewSize.Text = string.Format("0x{0:X}", count * _block.ElementSize);
		}

		private void BtnContinue_OnClick(object sender, RoutedEventArgs e)
		{
			int newCount;
			if (!int.TryParse(txtNewCount.Text, out newCount))
			{
				MetroMessageBox.Show("Tag Block Reallocator - Assembly", "Please enter a valid element count.");
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
			btnContinue.IsEnabled = (newCount != _originalCount);
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

		private void BtnZero_OnClick(object sender, RoutedEventArgs e)
		{
			txtNewCount.Text = "0";
		}

		private void BtnAddMore_OnClick(object sender, RoutedEventArgs e)
		{
			var deltaStr = MetroInputBox.Show("Tag Block Reallocator - Assembly",
				"Enter the number of elements to add to the block.\nEntering a negative number will remove elements from the end of the block.\nHexadecimal values starting with \"0x\" are allowed.",
				"", "Enter a number.", "^-?(0x[0-9a-f]+|[0-9]+)$");
			if (string.IsNullOrEmpty(deltaStr))
				return;
			int delta;
			if (TryParseDeltaString(deltaStr, out delta))
				AddToCount(delta);
		}

		private void AddToCount(int delta)
		{
			int newCount;
			if (!int.TryParse(txtNewCount.Text, out newCount))
				return;
			newCount = Math.Max(0, newCount + delta);
			txtNewCount.Text = newCount.ToString();
		}

		private static bool TryParseDeltaString(string str, out int result)
		{
			if (str.StartsWith("0x"))
			{
				// Positive hex number
				return int.TryParse(str.Substring(2), NumberStyles.HexNumber, null, out result);
			}
			if (str.StartsWith("-0x"))
			{
				// Negative hex number
				if (!int.TryParse(str.Substring(3), NumberStyles.HexNumber, null, out result))
					return false;
				result = -result;
				return true;
			}
			return int.TryParse(str, out result);
		}
	}
}