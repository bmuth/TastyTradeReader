using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace TastyTradeReader
{
    public class FeedDict : Dictionary<DateTime, FeedItem>
    {}

    public class Feed : List<FeedItem>
    { }

    public class FeedItem : INotifyPropertyChanged
    {
        private bool _ifdownloaded;

        public event PropertyChangedEventHandler PropertyChanged;


        public bool IfDownloaded
        {
            get
            {
                return _ifdownloaded;
            }
            set
            {
                if (_ifdownloaded != value)
                {
                    _ifdownloaded = value;
                    this.NotifyPropertyChanged ("IfDownloaded");
                    this.NotifyPropertyChanged ("IfNotDownloaded");
                }
            }
        }

        public bool IfNotDownloaded
        {
            get
            {
                return !_ifdownloaded;
            }
            //set
            //{
            //    if (_ifdownloaded != value)
            //    {
            //        _ifdownloaded = !value;
            //        this.NotifyPropertyChanged ("IfDownloaded");
            //        this.NotifyPropertyChanged ("IfNotDownloaded");
            //    }
            //}
        }

        private void NotifyPropertyChanged (string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs (propName));
            }
        }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Image { get; set; }
        public string  Movie { get; set; }
        public string RemoteImage { get; set; }
        public string RemoteMovie { get; set; }
        public string LocalImage { get; set; }
        public string LocalMovie { get; set; }
        public DateTime PubDate { get; set; }

        public FeedItem () { }

        public FeedItem (string title, string subtitle, string image, string movie, DateTime pubdate)
        {
            Title = title;
            Subtitle = subtitle;
            RemoteImage = image;
            RemoteMovie = movie;
            Image = RemoteImage;
            Movie = RemoteMovie;
            PubDate = pubdate;
        }

        public override string ToString ()
        {
            return String.Format ("{0} {1}", Title.Substring (0, Math.Min (Title.Length, 20)), PubDate);
        }

    }

    public class IfDownloadedToImageconverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof (Visibility))
            {
                throw new InvalidOperationException ("The target must be a Visibility");
            }

            //if (((bool) value)) // if downloaded
            //{
            //    u = "pack://application:,,,/TastyTradeReader;component/Images/repeat_download.png";
            //}
            //else
            //{
            //    u = "pack://application:,,,/TastyTradeReader;component/Images/download.png";
            //}
            //return new BitmapImage (new Uri (u));

            if ((bool) value)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }

        }
        public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
