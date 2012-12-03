using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ExtryzeDLL.IO;

namespace Assembly.Backend
{
    public class _0xabad1dea
    {
        public class IWff
        {
            public static bool Heman(EndianStream stream)
            {
                try
                {
                    if (Settings.applicationEasterEggs)
                    {
                        //if (CheckFileIsDownloaded())
                        //{
                        stream.SeekTo(0x00);
                        if (stream.ReadAscii(0x04) == "IWff")
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

            //public static bool CheckFileIsDownloaded()
            //{
            //    try
            //    {
            //        FileInfo fi = new FileInfo(GetFileLocation());
            //        return (fi.Length == 11787629);
            //    }
            //    catch { return false; }
            //}
            //public static string GetFileLocation()
            //{
            //    return VariousFunctions.GetTemporaryErrorLogs() + "IWff.etemp";
            //}
            //public static void BeginSilentFileDownload()
            //{
            //    if (Settings.applicationEasterEggs)
            //    {
            //        WebClient wb = new WebClient();
            //        wb.DownloadFileAsync(new Uri("http://assembly.xboxchaos.com/kbdata/IWff.etmp"), GetFileLocation());
            //    }
            //}

            public static void CleanUp()
            {
                Settings.homeWindow.maskingIWff.Visibility = Visibility.Collapsed;
                Settings.homeWindow.mediaIWff.Stop();
                Settings.homeWindow.mediaIWff.Position = new TimeSpan(0);
            }
        }

        public class TragicSans
        {
            public static IList<char> histroyOfEggs = new List<char>();

            public static void KeyDown(Key key)
            {
                if (Settings.applicationEasterEggs)
                {
                    if (histroyOfEggs.Count > 20)
                        histroyOfEggs.RemoveAt(20);

                    KeyConverter keey = new KeyConverter();
                    histroyOfEggs.Insert(0, keey.ConvertToString(null, System.Threading.Thread.CurrentThread.CurrentUICulture, key).ToLower().ToCharArray()[0]);

                    ValidateHistory();
                }
            }
            private static void ValidateHistory()
            {
                char[] fakeHistory = histroyOfEggs.ToArray<char>();
                Array.Reverse(fakeHistory);
                string realHistory = new string(fakeHistory).ToLower();
                
                if (realHistory.EndsWith("comic"))
                    try { App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Backend/0xabad1dea/TragicSans.xaml", UriKind.Relative) }); }
                    catch { }
                else if (realHistory.EndsWith("dingdong"))
                    try { App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Backend/0xabad1dea/DingDong.xaml", UriKind.Relative) }); }
                    catch { }
                // This isn't working, does WPF not support Halo fonts? Racist.
                //else if (realHistory.EndsWith("galoreacharound"))
                //    try { App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Backend/0xabad1dea/Galo.xaml", UriKind.Relative) }); }
                //    catch { }
                else if (realHistory.EndsWith("wtfisthis"))
                    try { App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Assembly;component/Metro/Controls/MetroFonts.xaml", UriKind.Relative) }); }
                    catch { }
            }
        }

        public class GodOfAllThingsHolyAndModdy
        {
            public static Visibility ShowGod()
            {
                if (Settings.applicationEasterEggs)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            public static string ShowGodsRealName()
            {
                if (Settings.applicationEasterEggs)
                    return "Morgan Freeman";
                else
                    return "Lord Zedd";
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