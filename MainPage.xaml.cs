using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace TastyTradeReader
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private Feed TotalFeed;

        public MainPage ()
        {
            InitializeComponent ();
        }

        private async void MainPageLoaded (object sender, RoutedEventArgs e)
        {
            TotalFeed = await FetchTastyTradeFeed ();
            FeedGrid.DataContext = TotalFeed.GetRange (0, 50);
        }

        private async Task<Feed> FetchTastyTradeFeed ()
        {
            Feed feed = new Feed ();

            XElement root = await LoadXDocumentAsync ("https://feeds.tastytrade.com/podcast.rss");
            XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

            var items = (from el in root.Descendants ("item")
                         select el
                         ).ToList ();

            foreach (var el in items)
            {
                string movie = el.Element ("guid").Value;
                if (!movie.EndsWith (".mov"))
                {
                    continue;
                }

                FeedItem fi = new FeedItem (el.Element ("title").Value,
                                              el.Element (itunes + "subtitle").Value,
                                              el.Element (itunes + "image").Attribute ("href").Value,
                                              el.Element ("guid").Value,
                                              DateTime.Parse (el.Element ("pubDate").Value));
                feed.Add (fi);
            }
 
            return feed;
        }

        private async Task<XElement> LoadXDocumentAsync (string url)
        {
            WebClient client = new WebClient ();
            Uri uri = new Uri (url);
            XElement x = XElement.Parse (await client.DownloadStringTaskAsync (uri));
            return x;
        }

        private void Button_Click (object sender, RoutedEventArgs e)
        {

        }
    }
}
