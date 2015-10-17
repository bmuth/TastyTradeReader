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

        public VideoWindow ()
        {
            InitializeComponent ();

        }

        public VideoWindow (FeedItem fi)
        {
            m_fi = fi;

            InitializeComponent ();

            ucMediaPlayer1.ImageFile = fi.Image;
            ucMediaPlayer1.MovieFile = fi.LocalMovie;
        }

        private void Video_Window_OnLoaded (object sender, RoutedEventArgs e)
        {
        }
    }
}
