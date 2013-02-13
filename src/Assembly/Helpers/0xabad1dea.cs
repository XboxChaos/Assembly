using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ExtryzeDLL.IO;

namespace Assembly.Helpers
{
    public class _0xabad1dea
    {
        public class IWff
        {
            public static bool Heman(EndianReader reader)
            {
                try
                {
                    if (Settings.applicationEasterEggs)
                    {
                        //if (CheckFileIsDownloaded())
                        //{
                        reader.SeekTo(0x00);
                        if (reader.ReadUInt32() == 1230464614)
                        {
                            // Play Video
                            Settings.homeWindow.mediaIWff.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
                            Settings.homeWindow.mediaIWff.Source = new Uri("http://assembly.xboxchaos.com/kbdata/IWff.etmp");
                            Settings.homeWindow.maskingIWff.Visibility = Visibility.Visible;
                            Settings.homeWindow.mediaIWff.Play();
                            Settings.homeWindow.mediaIWff.MediaEnded += (o, args) =>
                                {
                                    Settings.homeWindow.mediaIWff.Position = new TimeSpan(0);
                                    Settings.homeWindow.mediaIWff.Play();
                                };

                            return true;
                        }
                        //}
                    }

                    return false;
                }
                catch { return false; }
            }

            public static void CleanUp()
            {
                Settings.homeWindow.maskingIWff.Visibility = Visibility.Collapsed;
                Settings.homeWindow.mediaIWff.Stop();
                Settings.homeWindow.mediaIWff.Position = new TimeSpan(0);
            }
        }

        public class TragicSans
        {
            public static readonly IList<char> histroyOfEggs = new List<char>();

            public static void KeyDown(Key key)
            {
	            if (!Settings.applicationEasterEggs) return;

	            if (histroyOfEggs.Count > 20)
		            histroyOfEggs.RemoveAt(20);

	            var keey = new KeyConverter();
	            var convertToString = keey.ConvertToString(null, System.Threading.Thread.CurrentThread.CurrentUICulture, key);
	            if (convertToString != null)
		            histroyOfEggs.Insert(0, convertToString.ToLower().ToCharArray()[0]);

	            ValidateHistory();
            }
            private static void ValidateHistory()
            {
                var fakeHistory = histroyOfEggs.ToArray<char>();
                Array.Reverse(fakeHistory);
                var realHistory = new string(fakeHistory).ToLower();
                
                if (realHistory.EndsWith("comic"))
                    try { Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Helpers/0xabad1dea/TragicSans.xaml", UriKind.Relative) }); }
                    catch { }
                else if (realHistory.EndsWith("dingdong"))
                    try { Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Helpers/0xabad1dea/DingDong.xaml", UriKind.Relative) }); }
                    catch { }
                // This isn't working, does WPF not support Halo fonts? Racist.
                //else if (realHistory.EndsWith("galoreacharound"))
                //    try { App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Helpers/0xabad1dea/Galo.xaml", UriKind.Relative) }); }
                //    catch { }
                else if (realHistory.EndsWith("wtfisthis"))
                    try { Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Controls/MetroFonts.xaml", UriKind.Relative) }); }
                    catch { }
            }
        }

        public class GodOfAllThingsHolyAndModdy
        {
            public static Visibility ShowGod()
            {
	            return Settings.applicationEasterEggs ? Visibility.Visible : Visibility.Collapsed;
            }

	        public static string ShowGodsRealName()
	        {
		        return Settings.applicationEasterEggs ? "Morgan Freeman" : "Lord Zedd";
	        }
        }

        public static void CheckServerStatus()
        {
            if (Settings.applicationEasterEggs)
            {
                //if (!IWff.CheckFileIsDownloaded())
                //    IWff.BeginSilentFileDownload();
            }
        }
    }
}