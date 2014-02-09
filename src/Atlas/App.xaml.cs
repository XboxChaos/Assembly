using System.Windows;
using Atlas.Helpers;

namespace Atlas
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public static Storage Storage { get; set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			Storage = new Storage();
			
			base.OnStartup(e);
		}
	}
}
