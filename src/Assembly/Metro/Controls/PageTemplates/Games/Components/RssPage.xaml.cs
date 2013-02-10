using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Assembly.Metro.Controls.PageTemplates.Games.Components
{
    /// <summary>
    /// Interaction logic for RssPage.xaml
    /// </summary>
    public partial class RssPage : UserControl
    {
        public Uri RSSFeed { get; set; }
        public ObservableCollection<RSSEntry> RssItems = new ObservableCollection<RSSEntry>();

        public RssPage(Uri rssFeed)
        {
            InitializeComponent();
            this.DataContext = RssItems;

            RSSFeed = rssFeed;

            // Grab RSS Feed
            Thread thrd = new Thread(new ThreadStart(GrabRSS));
            thrd.SetApartmentState(ApartmentState.STA);
            thrd.Start();
        }

        public void GrabRSS()
        {
            try
            {
                XmlNode nodeRss = null;
                XmlNode nodeChannel = null;
                XmlNode nodeItem = null;
                WebClient wb = new WebClient();

                // Read the RSS XML into an XmlReader, then proceed to fuck bitches
                XmlTextReader rssReader = new XmlTextReader(RSSFeed.OriginalString);
                XmlDocument rssDoc = new XmlDocument();
                rssDoc.Load(rssReader);

                // Get the rss tag
                for (int i = 0; i < rssDoc.ChildNodes.Count; i++)
                    if (rssDoc.ChildNodes[i].Name == "rss")
                        nodeRss = rssDoc.ChildNodes[i];

                // Get the chanel tag
                for (int i = 0; i < nodeRss.ChildNodes.Count; i++)
                    if (nodeRss.ChildNodes[i].Name == "channel")
                        nodeChannel = nodeRss.ChildNodes[i];

                // Get all the items
                for (int i = 0; i < nodeChannel.ChildNodes.Count; i++)
                    if (nodeChannel.ChildNodes[i].Name == "item")
                    {
                        nodeItem = nodeChannel.ChildNodes[i];

                        RSSEntry rssEntry = new RSSEntry();
                        rssEntry.Title = nodeItem["title"].InnerText;
                        rssEntry.Link = nodeItem["link"].InnerText;
                        rssEntry.Description = nodeItem["description"].InnerText;
                        rssEntry.PubDate = DateTime.Parse(nodeItem["pubDate"].InnerText);
                        rssEntry.FriendlyPubDate = rssEntry.PubDate.ToString("dddd, dd MMMM yyyy");
                        rssEntry.GUID = nodeItem["guid"].InnerText;

                        Dispatcher.Invoke(new Action(delegate 
                            {
                                RssItems.Add(rssEntry);
                            }));
                    }
            }
            catch
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    // No internet connection :(, tell the user they are a jewish bastard.
                    tutorialRSSList.Visibility = System.Windows.Visibility.Collapsed;
                    gridNoConnection.Visibility = System.Windows.Visibility.Visible;
                }));
            }
        }

        public class RSSEntry
        {
            public string Title { get; set; }
            public string Link { get; set; }
            public string Description { get; set; }
            public DateTime PubDate { get; set; }
            public string FriendlyPubDate { get; set; }
            public string GUID { get; set; }
        }

        private void tutorialRSSList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (tutorialRSSList.SelectedItem != null && tutorialRSSList.SelectedItem is RSSEntry)
                Process.Start(((RSSEntry)tutorialRSSList.SelectedItem).Link);
        }
    }
}
