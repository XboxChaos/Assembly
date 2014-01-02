using System;

namespace Atlas.Pages
{
	/// <summary>
	/// Interaction logic for StartPage.xaml
	/// </summary>
	public partial class StartPage : IAssemblyPage
	{
		public StartPage()
		{
			InitializeComponent();
		}

		public bool Close()
		{
			return true;
		}
	}
}
