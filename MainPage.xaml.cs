using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Reflection;
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
using System.Xml.Serialization;

namespace TastyTradeReader
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private List<FeedItem> DisplayedFeed;
        private List<FeedItem> TotalFeed;
        private FeedDict m_FeedDictionary;

        public MainPage ()
        {
            InitializeComponent ();

            List<string> man = new List<string> ();
            Assembly assembly = Assembly.GetExecutingAssembly ();
            foreach (string s in assembly.GetManifestResourceNames ())
            {
                man.Add (s);
            }
            int x = 4;
        }

        private async void MainPageLoaded (object sender, RoutedEventArgs e)
        {
            FetchLocalFeed ();
            Feed feed = await FetchTastyTradeFeed ();

            foreach (FeedItem fi in feed)
            {
                if (!m_FeedDictionary.ContainsKey (fi.PubDate))
                {
                    m_FeedDictionary[fi.PubDate] = fi;
                }
            }

            TotalFeed = (from pair in m_FeedDictionary
                         orderby pair.Key descending
                         select pair.Value).ToList ();

            DisplayedFeed = TotalFeed.GetRange (0, 50);
            FeedGrid.DataContext = DisplayedFeed;
        }

        private void FetchLocalFeed ()
        {
            m_FeedDictionary = new FeedDict ();

            string path = (string) App.Current.Resources["PodcastPath"];
            var folders = Directory.EnumerateDirectories (path);
            m_FeedDictionary = new FeedDict ();
            foreach (var folder in folders)
            {
                string f = new DirectoryInfo (folder).Name;
                DateTime dt = DateTime.ParseExact (f, "yyyy-MMM-dd HHmm", CultureInfo.InvariantCulture);
                StreamReader sr = new StreamReader (folder + "\\feed.xml");
                XmlSerializer xr = new XmlSerializer (typeof (FeedItem));
                FeedItem fi = (FeedItem) xr.Deserialize (sr);
                m_FeedDictionary[dt] = fi;
            }
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

         private async void Button_Click (object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            FeedItem fi = button.Tag as FeedItem;
            DependencyObject obj = (DependencyObject) sender;

            while (!(obj is ListViewItem))
            {
                obj = VisualTreeHelper.GetParent (obj);
            }
            ListViewItem vi = (ListViewItem) obj;
            ProgressBar pb = vi.GetChildOfType<ProgressBar> ();
            button.Visibility = Visibility.Hidden;
            pb.Visibility = Visibility.Visible;

            await DownloadMovie (fi, pb);

            pb.Visibility = Visibility.Hidden;
            button.Visibility = Visibility.Visible;

            Image image = button.GetChildOfType<Image> ();

////            Uri uri = new Uri ("ms-appx://TastyTradeReader;component/Images/repeat_download.png");
//            Uri uri = new Uri ("pack://application:,,,/TastyTradeReader;component/Images/repeat_download.png");
//            BitmapImage bitmapImage = new BitmapImage (uri);
//            image.Source = bitmapImage;
        }

        private async Task DownloadMovie (FeedItem fi, ProgressBar pb)
        {
            Uri uri = new Uri (fi.Movie);

            string filename = CreateFileName (fi);

            using (WebClient client = new WebClient ())
            {
                client.DownloadProgressChanged += (o, e) =>
                {
                    pb.Value = e.ProgressPercentage;
                };

                await client.DownloadFileTaskAsync (uri, filename);
                fi.LocalMovie = filename;

                XmlSerializer ser = new XmlSerializer (typeof (FeedItem));
                using (TextWriter writer = new StreamWriter (System.IO.Path.GetDirectoryName (filename) + "\\feed.xml"))
                {
                    ser.Serialize (writer, fi);
                    writer.Close ();
                }
            }
        }

        private string CreateFileName (FeedItem fi)
        {
            string path = (string) App.Current.Resources["PodcastPath"];
            string dir = fi.PubDate.ToString ("yyyy-MMM-dd HHmm");
            path += dir;
            try
            {
                Directory.CreateDirectory (path);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
            path += "\\";
            string name = fi.Movie.Substring (fi.Movie.LastIndexOf ('/') + 1);
            path += name;
            return path;
        }

        private void Play_Click (object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            FeedItem fi = button.Tag as FeedItem;

            if (!fi.IfDownloaded)
            {
                MessageBox.Show (string.Format ("This file needs to be downloaded first. \r\n{0}", fi.Title.Substring (0, 40)));
                return;
            }

            VideoWindow vw = new VideoWindow (fi);
            vw.Show ();
        }
    }
}
