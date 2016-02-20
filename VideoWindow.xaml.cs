using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TastyTradeReader
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        FeedItem m_fi;
        bool bSaveSwitch = false;

        public VideoWindow ()
        {
            InitializeComponent ();

        }

        public void StartFeed (FeedItem fi)
        {
            m_fi = fi;

            InitializeComponent ();

            ucMediaPlayer1.ImageFile = fi.RemoteImage;
            ucMediaPlayer1.MovieFile = fi.LocalMovie;
        }

        private void Video_Window_OnLoaded (object sender, RoutedEventArgs e)
        {
            this.Height = Properties.Settings.Default.VidHeight;
            this.Width = Properties.Settings.Default.VidWidth;
            bSaveSwitch = true;
        }

        private void Window_SizeChanged (object sender, SizeChangedEventArgs e)
        {
            if (bSaveSwitch)
            {
                Properties.Settings.Default.VidHeight = this.Height;
                Properties.Settings.Default.VidWidth = this.Width;
                Properties.Settings.Default.Save ();
            }
        }
    }
}
