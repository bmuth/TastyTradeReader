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
    public partial class MainWindow : Window
    {
        private List<FeedItem> m_DisplayedFeed;
        private List<FeedItem> m_FavouriteFeed;
        private List<FeedItem> m_DomainFeed;  // either local or remote
        private FeedDict m_FeedDictionary;
        private int CurrentOffset = 0;

        public MainWindow ()
        {
            InitializeComponent ();

            List<string> man = new List<string> ();
            Assembly assembly = Assembly.GetExecutingAssembly ();
            foreach (string s in assembly.GetManifestResourceNames ())
            {
                man.Add (s);
            }
        }

        private void MainPageLoaded (object sender, RoutedEventArgs e)
        {
            GetFullFeed ();
        }

        private async void GetFullFeed ()
        {
            FetchLocalFeedAsDictionary ();
            Feed feed = await FetchTastyTradeFeed ();

            foreach (FeedItem fi in feed)
            {
                if (!m_FeedDictionary.ContainsKey (fi.PubDate))
                {
                    m_FeedDictionary[fi.PubDate] = fi;
                }
            }

            m_DomainFeed = (from pair in m_FeedDictionary
                           orderby pair.Key descending
                           select pair.Value).ToList ();

            m_FavouriteFeed = (from fi in m_DomainFeed where Favourited (fi) select fi).ToList ();
            m_DisplayedFeed = m_DomainFeed.GetRange (CurrentOffset, Math.Min (m_DomainFeed.Count, 50));
            CurrentOffset = 0;
            FeedGrid.DataContext = m_DisplayedFeed;
        }

        private void FetchLocalFeedAsDictionary ()
        {
            m_FeedDictionary = new FeedDict ();

            string path = Properties.Settings.Default.PodcastPath;

            try
            {
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
            catch (Exception e)
            {
                MessageBox.Show (string.Format ("Failed to enumerate locally downloaded podcasts. {0}", e.Message));
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

        private async void Download_Click (object sender, RoutedEventArgs e)
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
            //button.Visibility = Visibility.Hidden;
            pb.Visibility = Visibility.Visible;

            await DownloadMovie (fi, pb);

            //            pb.Visibility = Visibility.Hidden;
            //button.Visibility = Visibility.Visible;

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

            VideoWindow vw = new VideoWindow ();
            vw.StartFeed (fi);
            vw.Title = fi.Title;
            vw.Show ();
        }

        private void btnOnlyDownloaded_Click (object sender, RoutedEventArgs e)
        {
            foreach (FeedItem fi in ListShows.SelectedItems)
            {
                ListViewItem vi = ListShows.ItemContainerGenerator.ContainerFromItem (fi) as ListViewItem;
                ProgressBar pb = vi.GetChildOfType<ProgressBar> ();
                pb.Visibility = Visibility.Visible;
                DownloadMovie (fi, pb);
            }
        }

        private void btnFavourites_Click (object sender, RoutedEventArgs e)
        {
            FeedGrid.DataContext = null;

            if (btnFavourites.IsChecked == true)
            {
                if (m_FavouriteFeed.Count - CurrentOffset > 0)
                {
                    m_DisplayedFeed = m_FavouriteFeed.GetRange (CurrentOffset, Math.Min (m_FavouriteFeed.Count - CurrentOffset, 50));
                }
            }
            else
            {
                if (m_DomainFeed.Count - CurrentOffset > 0)
                {
                    m_DisplayedFeed = m_DomainFeed.GetRange (CurrentOffset, Math.Min (m_DomainFeed.Count - CurrentOffset, 50));
                }
            }

            FeedGrid.DataContext = m_DisplayedFeed;
        }

        private bool Favourited (FeedItem fi)
        {
            string[] titles = {"Last Call",
                                "The Project",
                                "The Webinar",
                                "Talkin' With Tom and Tony",
                                "Good Trade Bad Trade",
                                "Market Measures",
                                "Options Jive",
                                "The Skinny On Options Modeling",
                                "Trades From the Research Team",
                                "IRA Options",
                                "Trade Small Trade Often",
                                "Top Dogs",
                                "Strategies for IRA",
                                "Options:",
                                "Calling All Millionaires",
                                "tasty BITES"
            };
            foreach (var title in titles)
            {
                if (fi.Title.IndexOf (title, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void btnBack_Click (object sender, RoutedEventArgs e)
        {
            int old = CurrentOffset;
            CurrentOffset -= 50;
            if (CurrentOffset < 0)
            {
                CurrentOffset = 0;
            }
            if (old != CurrentOffset)
            {
                btnFavourites_Click (null, null);
            }
        }

        private void btnForward_Click (object sender, RoutedEventArgs e)
        {
            int old = CurrentOffset;
            CurrentOffset += 50;
            if (CurrentOffset > m_DomainFeed.Count)
            {
                CurrentOffset = m_DomainFeed.Count;
            }
            if (old != CurrentOffset)
            {
                btnFavourites_Click (null, null);
            }
        }

        private void ProgressChanged_ValueChanged (object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ProgressBar pb = sender as ProgressBar;
            pb.Visibility = (pb.Value == 0 || pb.Value == 100) ? Visibility.Hidden : Visibility.Visible;
        }


        private void Delete_Click (object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            FeedItem fi = button.Tag as FeedItem;

            DirectoryInfo dir = new DirectoryInfo (System.IO.Path.GetDirectoryName (fi.LocalMovie));
            try
            {
                dir.Delete (true);
                fi.LocalMovie = null;
                fi.IfDownloaded = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show (string.Format ("Failed to delete {0}. {1}", dir.Name, ex.Message));
            }
        }

        private void btnLocalFiles_Click (object sender, RoutedEventArgs e)
        {
            if (btnLocalFiles.IsChecked == true)
            {
                m_DomainFeed = new List<FeedItem> ();

                string path = (string) App.Current.Resources["PodcastPath"];
                var folders = Directory.EnumerateDirectories (path);

                foreach (var folder in folders)
                {
                    string f = new DirectoryInfo (folder).Name;
                    DateTime dt = DateTime.ParseExact (f, "yyyy-MMM-dd HHmm", CultureInfo.InvariantCulture);
                    StreamReader sr = new StreamReader (folder + "\\feed.xml");
                    XmlSerializer xr = new XmlSerializer (typeof (FeedItem));
                    FeedItem fi = (FeedItem) xr.Deserialize (sr);
                    m_DomainFeed.Add (fi);
                }

                m_DomainFeed = m_DomainFeed.OrderByDescending ((s) => s.PubDate).ToList ();
                CurrentOffset = 0;

                m_FavouriteFeed = (from fi in m_DomainFeed where Favourited (fi) select fi).ToList ();
                m_DisplayedFeed = m_DomainFeed.GetRange (CurrentOffset, Math.Min (m_DomainFeed.Count, 50));

                FeedGrid.DataContext = m_DisplayedFeed;
            }
            else
            {
                GetFullFeed ();
            }
        }
    }
}
