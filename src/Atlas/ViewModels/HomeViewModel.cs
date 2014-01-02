using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Atlas.Metro.Controls.Custom;
using Atlas.Pages;

namespace Atlas.ViewModels
{
	public class HomeViewModel : MetroWindow, INotifyPropertyChanged
	{
		#region Properties

		#region Application Stuff

		public string Status
		{
			get { return _status; }
			set { SetField(ref _status, value); }
		}
		private string _status = "Ready...";

		public string ApplicationTitle
		{
			get { return _applicationTitle; }
			set { SetField(ref _applicationTitle, value); }
		}
		private string _applicationTitle = "Welcome";

		public Thickness ApplicationBorderThickness
		{
			get { return _applicationBorderThickness; }
			set { SetField(ref _applicationBorderThickness, value); }
		}
		private Thickness _applicationBorderThickness;

		#endregion

		#region Window Masks

		public Visibility MaskVisibility
		{
			get { return _maskVisibility; }
			set { SetField(ref _maskVisibility, value); }
		}
		private Visibility _maskVisibility = Visibility.Collapsed;

		#endregion

		#region Window Actions/Resizing

		public Visibility ActionRestoreVisibility
		{
			get { return _actionRestoreVisibility; }
			set { SetField(ref _actionRestoreVisibility, value); }
		}
		private Visibility _actionRestoreVisibility = Visibility.Collapsed;

		public Visibility ActionMaximizeVisibility
		{
			get { return _actionMaximizeVisibility; }
			set { SetField(ref _actionMaximizeVisibility, value); }
		}
		private Visibility _actionMaximizeVisibility = Visibility.Collapsed;

		public Visibility ResizingVisibility
		{
			get { return _resizingVisibility; }
			set { SetField(ref _resizingVisibility, value); }
		}
		private Visibility _resizingVisibility = Visibility.Collapsed;

		#endregion

		#region Content

		public IAssemblyPage AssemblyPage
		{
			get { return _assemblyPage; }
			set { SetField(ref _assemblyPage, value); }
		}
		private IAssemblyPage _assemblyPage = new StartPage();
		#endregion

		#endregion

		#region Events

		#region Overrides
		public void OnStateChanged(object sender, EventArgs eventArgs)
		{
			switch ((WindowState)sender)
			{
				case WindowState.Normal:
					ApplicationBorderThickness = new Thickness(1, 1, 1, 25);
					ActionRestoreVisibility = Visibility.Collapsed;
					ActionMaximizeVisibility = 
						ResizingVisibility = Visibility.Visible;
					break;
				case WindowState.Maximized:
					ApplicationBorderThickness = new Thickness(0, 0, 0, 25);
					ActionRestoreVisibility = Visibility.Visible;
					ActionMaximizeVisibility =
						ResizingVisibility = Visibility.Collapsed;
					break;
			}
		}
		#endregion

		#endregion

		#region Binding

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		protected bool SetField<T>(ref T field, T value,
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
