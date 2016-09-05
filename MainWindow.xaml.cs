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
        //private FeedDict m_FeedDictionary;
        private int CurrentOffset = 0;
        private string RootDir;

        public MainWindow ()
        {
            InitializeComponent ();

            RootDir = Properties.Settings.Default.PodcastPath;

            /* Make sure it terminates in a \
             * ------------------------------ */

            if (RootDir[RootDir.Length - 1] != '\\')
            {
                RootDir += "\\";
            }
            Directory.SetCurrentDirectory (RootDir);

            List<string> man = new List<string> ();
            Assembly assembly = Assembly.GetExecutingAssembly ();
            foreach (string s in assembly.GetManifestResourceNames ())
            {
                man.Add (s);
            }
        }

        /*************************************************************
        *
        * MainPageLoaded
        *
        *************************************************************/

        private void MainPageLoaded (object sender, RoutedEventArgs e)
        {
            GetFullFeed ();
        }

        /*************************************************************
        *
        * GetFullFeed
        *
        *************************************************************/

        private async void GetFullFeed ()
        {
            FeedDict fd = FetchLocalFeedAsDictionary ();
            Feed feed = await FetchTastyTradeFeed ();

            foreach (FeedItem fi in feed)
            {
                if (!fd.ContainsKey (fi.PubDate))
                {
                    fd[fi.PubDate] = fi;
                }
            }

            m_DomainFeed = (from pair in fd
                           orderby pair.Key descending
                           select pair.Value).ToList ();

            m_FavouriteFeed = (from fi in m_DomainFeed where Favourited (fi) select fi).ToList ();
            m_DisplayedFeed = m_DomainFeed.GetRange (CurrentOffset, Math.Min (m_DomainFeed.Count, 50));
            CurrentOffset = 0;
            FeedGrid.DataContext = m_DisplayedFeed;
        }

        /*************************************************************
        *
        * FetchLocalFeedAsDictionary
        *
        *************************************************************/

        private FeedDict FetchLocalFeedAsDictionary ()
        {
            FeedDict fd = new FeedDict ();

            string path = RootDir;

            try
            {
                var folders = Directory.EnumerateDirectories (path);
                foreach (var folder in folders)
                {
                    DateTime dt;
                    FeedItem fi = null;
                    string f = new DirectoryInfo (folder).Name;
                    try
                    {
                        dt = DateTime.ParseExact (f, "yyyy-MMM-dd HHmm", CultureInfo.InvariantCulture);
                    }
                    catch (Exception )
                    {
                        continue;
                    }
                    try
                    {
                        using (StreamReader sr = new StreamReader (folder + "\\feed.xml"))
                        {
                            XmlSerializer xr = new XmlSerializer (typeof (FeedItem));
                            fi = (FeedItem) xr.Deserialize (sr);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show (string.Format ("Failed to deserialize {0}. {1}. Will skip", folder + "\\feed.xml", ex.Message));
                        continue;
                    }
                    fd[dt] = fi;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show (string.Format ("Failed to enumerate locally downloaded podcasts. {0}", e.Message));
            }
            return fd;
        }

        /*************************************************************
        *
        * async Task<Feed> FetchTastyTradeFeed
        *
        *************************************************************/

        private async Task<Feed> FetchTastyTradeFeed ()
        {
            Feed feed = new Feed ();

            XElement root;

            try
            {
                root = await LoadXDocumentAsync ("https://feeds.tastytrade.com/podcast.rss");
            }
            catch (Exception e)
            {
                MessageBox.Show (string.Format ("Failed to access tastytrade podcasts. {0}", e.Message));
                return feed;
            }

            XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

            var items = (from el in root.Descendants ("item")
                         select el
                         ).ToList ();

            foreach (var el in items)
            {
                string movie = el.Element ("guid").Value;
                if (! (movie.EndsWith (".mov") || movie.EndsWith (".mp4")) )
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

        /*************************************************************
        *
        * async Task<XElement> LoadXDocumentAsync
        *
        *************************************************************/

        private async Task<XElement> LoadXDocumentAsync (string url)
        {
            WebClient client = new WebClient ();
            Uri uri = new Uri (url);
            XElement x = XElement.Parse (await client.DownloadStringTaskAsync (uri));
            return x;
        }

        /*************************************************************
        *
        * Download_Click
        *
        *************************************************************/

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

            await DownloadImageAndMovie (fi, pb);

            //            pb.Visibility = Visibility.Hidden;
            //button.Visibility = Visibility.Visible;

            Image image = button.GetChildOfType<Image> ();

            ////            Uri uri = new Uri ("ms-appx://TastyTradeReader;component/Images/repeat_download.png");
            //            Uri uri = new Uri ("pack://application:,,,/TastyTradeReader;component/Images/repeat_download.png");
            //            BitmapImage bitmapImage = new BitmapImage (uri);
            //            image.Source = bitmapImage;
        }

        /*************************************************************
        *
        * DownloadImageAndMovie
        *
        *************************************************************/

        private async Task DownloadImageAndMovie (FeedItem fi, ProgressBar pb)
        {

/*          download image
            -------------- */

            Uri ImageUri = new Uri (fi.RemoteImage);
            string image_filename = CreateFileNameForImage (fi);

            using (WebClient client = new WebClient ())
            {
                try
                {
                    client.DownloadFile (ImageUri, image_filename);
                }
                catch (Exception e)
                {
                    string msg = "Failed to download picture.";
                    while (e != null)
                    {
                        msg += "\r\n"+ e.Message;
                        e = e.InnerException;
                    }
                    MessageBox.Show (msg);
                    throw;

                }
                fi.LocalImage = image_filename;
            }

/*          download movie
            -------------- */

            Uri uri = new Uri (fi.RemoteMovie);
            string movie_filename = CreateFileNameForMovie (fi);

            using (WebClient client = new WebClient ())
            {
                client.DownloadProgressChanged += (o, e) =>
                {
                    pb.Value = e.ProgressPercentage;
                };

                await client.DownloadFileTaskAsync (uri, movie_filename);
                fi.LocalMovie = movie_filename;

                fi.Image = ConvertToUrlForm (fi.LocalImage);
                fi.Movie = ConvertToUrlForm (fi.LocalMovie);

                fi.IfDownloaded = true;

                XmlSerializer ser = new XmlSerializer (typeof (FeedItem));
                using (TextWriter writer = new StreamWriter (System.IO.Path.GetDirectoryName (movie_filename) + "\\feed.xml"))
                {
                    ser.Serialize (writer, fi);
                    writer.Close ();
                }
            }
        }

        /*************************************************************
        *
        * ConvertToUrlForm
        *
        *************************************************************/

        private string ConvertToUrlForm (string localImage)
        {
            StringBuilder sb = new StringBuilder ("file://");
            sb.Append (Directory.GetCurrentDirectory ().Replace ('\\', '/'));
            sb.Append ("/");
            sb.Append (localImage.Replace ('\\', '/'));
            return sb.ToString ();
        }

        /*************************************************************
        *
        * CreateFileNameForImage
        *
        *************************************************************/

        private string CreateFileNameForImage (FeedItem fi)
        {
            string path = RootDir; // (string) App.Current.Resources["PodcastPath"];
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
            dir += "\\";
            string name = fi.RemoteImage.Substring (fi.RemoteImage.LastIndexOf ('/') + 1);
            int quest = name.IndexOf ('?');
            if (quest >= 0)
            {
                name = name.Substring (0, quest);
            }
            dir += name;
            return dir;
        }

        /*************************************************************
        *
        * CreateFileNameForMovie
        *
        *************************************************************/

        private string CreateFileNameForMovie (FeedItem fi)
        {
            string path = RootDir; // (string) App.Current.Resources["PodcastPath"];
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
            dir += "\\";
            string name = fi.RemoteMovie.Substring (fi.RemoteMovie.LastIndexOf ('/') + 1);
            dir += name;
            return dir;
        }

        /*************************************************************
        *
        * Play
        *
        *************************************************************/

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

        /*************************************************************
        *
        * Download Selected Movies
        *
        *************************************************************/

        private void btnOnlyDownloaded_Click (object sender, RoutedEventArgs e)
        {
            foreach (FeedItem fi in ListShows.SelectedItems)
            {
                ListViewItem vi = ListShows.ItemContainerGenerator.ContainerFromItem (fi) as ListViewItem;
                ProgressBar pb = vi.GetChildOfType<ProgressBar> ();
                pb.Visibility = Visibility.Visible;
                DownloadImageAndMovie (fi, pb);
            }

            ListShows.UnselectAll ();
        }

        /*************************************************************
        *
        * Show Only Favourites
        *
        *************************************************************/

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

        /***************************************************************************
        *
        * LocalFiles only
        *
        ***************************************************************************/

        private void btnLocalFiles_Click (object sender, RoutedEventArgs e)
        {
            if (btnLocalFiles.IsChecked == true)
            {
                m_DomainFeed = new List<FeedItem> ();

                string path = RootDir; // (string) App.Current.Resources["PodcastPath"];
                var folders = Directory.EnumerateDirectories (path);

                foreach (var folder in folders)
                {
                    string f = new DirectoryInfo (folder).Name;
                    try
                    {
                        DateTime dt = DateTime.ParseExact (f, "yyyy-MMM-dd HHmm", CultureInfo.InvariantCulture);
                        using (StreamReader sr = new StreamReader (folder + "\\feed.xml"))
                        {
                            XmlSerializer xr = new XmlSerializer (typeof (FeedItem));
                            FeedItem fi = (FeedItem) xr.Deserialize (sr);

                            /* Update Image and Movie nodes
                               ---------------------------- */

                            fi.Image = ConvertToUrlForm (fi.LocalImage);
                            fi.Movie = ConvertToUrlForm (fi.LocalMovie);

                            m_DomainFeed.Add (fi);
                        }
                    }
                    catch (Exception )
                    {
                        // skip. could be irrelevant folder
                    }
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

        private void SetDir_Clicked (object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog ())
            {
                fd.Description = "Select the directory for viewing shows";
                fd.RootFolder = Environment.SpecialFolder.MyComputer;

                System.Windows.Forms.DialogResult rc = fd.ShowDialog ();
                if (rc == System.Windows.Forms.DialogResult.OK)
                {
                    RootDir = fd.SelectedPath;
                    Directory.SetCurrentDirectory (RootDir);
                }

                btnLocalFiles.IsChecked = true;
                btnLocalFiles_Click (null, null);

            }
        }

        /********************************************************************************
        *
        * Export clicked
        *
        ********************************************************************************/
        
        private async void Export_Clicked (object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog ())
            {
                fd.Description = "Select the directory for exporting selected shows";
                fd.RootFolder = Environment.SpecialFolder.MyComputer;

                System.Windows.Forms.DialogResult rc = fd.ShowDialog ();
                if (rc == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (FeedItem fi in ListShows.SelectedItems)
                    {
                        ListViewItem vi = ListShows.ItemContainerGenerator.ContainerFromItem (fi) as ListViewItem;
                        ProgressBar pb = vi.GetChildOfType<ProgressBar> ();
                        pb.Visibility = Visibility.Visible;
                        if (fi.IfNotDownloaded)
                        {
                            continue;
                        }
                        CopyImageAndXml (fi, fd.SelectedPath);
                        await CopyMovie (fi, pb, fd.SelectedPath);
                    }
                }
            }
        }

        private void CopyImageAndXml (FeedItem fi, string TargetPath)
        {
            if (!(TargetPath.EndsWith ("\\") || TargetPath.EndsWith ("/")))
            {
                TargetPath += '\\';
            }
            string path = System.IO.Path.GetDirectoryName (fi.LocalImage);
            string file = System.IO.Path.GetFileName (fi.LocalImage);

            int index = path.LastIndexOf ('\\');
            string datefolder = path.Substring (index + 1);

            string targetdir = TargetPath + datefolder + '\\';

            if (!System.IO.Directory.Exists (targetdir))
            {
                System.IO.Directory.CreateDirectory (targetdir);
            }

            File.Copy (fi.LocalImage, targetdir + file, true);
            File.Copy (path + "\\feed.xml", targetdir + "feed.xml", true);
        }

        private Task CopyMovie (FeedItem fi, ProgressBar pb, string TargetPath)
        {
            if (! (TargetPath.EndsWith ("\\") || TargetPath.EndsWith ("/")))
            {
                TargetPath += '\\';
            }
            string path = System.IO.Path.GetDirectoryName (fi.LocalMovie);
            string file = System.IO.Path.GetFileName (fi.LocalMovie);

            int index = path.LastIndexOf ('\\');
            string datefolder = path.Substring (index + 1);

            string targetdir = TargetPath + datefolder + '\\';

            if (!System.IO.Directory.Exists (targetdir))
            {
                System.IO.Directory.CreateDirectory (targetdir);
            }

            FileCopier fc = new FileCopier ( fi.LocalMovie, targetdir + file);

            fc.OnProgressChanged += (percentage) =>
            {
                Application.Current.Dispatcher.BeginInvoke (System.Windows.Threading.DispatcherPriority.Background, new Action (() => pb.Value = percentage));
            };

            fc.OnComplete += (() =>
            {
                Application.Current.Dispatcher.BeginInvoke (System.Windows.Threading.DispatcherPriority.Background, new Action (() => pb.Visibility = Visibility.Hidden));
            });

            return Task.Run (() =>
            {
                fc.Copy ();
            });
        }

        private void btnRefresh_Click (object sender, RoutedEventArgs e)
        {
            GetFullFeed ();
        }
    }
}
