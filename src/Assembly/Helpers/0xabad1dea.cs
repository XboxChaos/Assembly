using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Blamite.IO;

namespace Assembly.Helpers
{
	public class _0xabad1dea
	{
		public static void CheckServerStatus()
		{
			if (App.AssemblyStorage.AssemblySettings.ApplicationEasterEggs)
			{
				//if (!IWff.CheckFileIsDownloaded())
				//    IWff.BeginSilentFileDownload();
			}
		}

		public class GodOfAllThingsHolyAndModdy
		{
			public static Visibility ShowGod()
			{
				return App.AssemblyStorage.AssemblySettings.ApplicationEasterEggs ? Visibility.Visible : Visibility.Collapsed;
			}

			public static string ShowGodsRealName()
			{
				return App.AssemblyStorage.AssemblySettings.ApplicationEasterEggs ? "Morgan Freeman" : "Lord Zedd";
			}
		}

		public class IWff
		{
			public static bool Heman(EndianReader reader)
			{
				try
				{
					if (!App.AssemblyStorage.AssemblySettings.ApplicationEasterEggs) return false;

					reader.SeekTo(0x00);
					if (reader.ReadUInt32() != 1230464614) return false;

					// Play Video
					App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.LoadedBehavior = MediaState.Manual;
					App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Source =
						new Uri("http://assembly.xboxchaos.com/kbdata/IWff.etmp");
					App.AssemblyStorage.AssemblySettings.HomeWindow.maskingIWff.Visibility = Visibility.Visible;
					App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Play();
					App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.MediaEnded += (o, args) =>
					{
						App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Position = new TimeSpan(0);
						App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Play();
					};

					return true;
				}
				catch
				{
					return false;
				}
			}

			public static void CleanUp()
			{
				App.AssemblyStorage.AssemblySettings.HomeWindow.maskingIWff.Visibility = Visibility.Collapsed;
				App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Stop();
				App.AssemblyStorage.AssemblySettings.HomeWindow.mediaIWff.Position = new TimeSpan(0);
			}
		}

		public class TragicSans
		{
			public static readonly IList<char> histroyOfEggs = new List<char>();

			public static void KeyDown(Key key)
			{
				if (!App.AssemblyStorage.AssemblySettings.ApplicationEasterEggs) return;

				if (histroyOfEggs.Count > 20)
					histroyOfEggs.RemoveAt(20);

				var keey = new KeyConverter();
				string convertToString = keey.ConvertToString(null, Thread.CurrentThread.CurrentUICulture, key);
				if (convertToString != null)
					histroyOfEggs.Insert(0, convertToString.ToLower().ToCharArray()[0]);

				ValidateHistory();
			}

			private static void ValidateHistory()
			{
				char[] fakeHistory = histroyOfEggs.ToArray();
				Array.Reverse(fakeHistory);
				string realHistory = new string(fakeHistory).ToLower();

				if (realHistory.EndsWith("comic"))
					try
					{
						Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
						{
							Source = new Uri("/Assembly;component/Helpers/0xabad1dea/TragicSans.xaml", UriKind.Relative)
						});
					}
					catch
					{
					}
				else if (realHistory.EndsWith("dingdong"))
					try
					{
						Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
						{
							Source = new Uri("/Assembly;component/Helpers/0xabad1dea/DingDong.xaml", UriKind.Relative)
						});
					}
					catch
					{
					}
				else if (realHistory.EndsWith("wtfisthis"))
					try
					{
						Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
						{
							Source = new Uri("/Assembly;component/Metro/Controls/MetroFonts.xaml", UriKind.Relative)
						});
					}
					catch
					{
					}
			}
		}
	}
}