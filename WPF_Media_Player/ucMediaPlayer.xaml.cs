using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;


namespace WPF_Media_Player
{
    /// <summary>
    /// prvoides a media player with a track list and volume controls and rating system
    /// </summary>
    public partial class ucMediaPlayer : System.Windows.Controls.UserControl
    {
        #region Instance Fields
        // private Dictionary<string, MediaItem> mediaItems = new Dictionary<string, MediaItem>();
        ToggleButton[] btnStars = new ToggleButton[5];
        #endregion
        #region Ctor
        public ucMediaPlayer ()
        {
            InitializeComponent ();

            sliderTime.IsEnabled = false;
            sliderVolume.IsEnabled = false;
        }
        #endregion

        #region Public properties

        public string ImageFile { get; set; }
        public string MovieFile { get; set; }

        #endregion

        #region Private methods

        /// <summary>
        /// Stop media when ended
        /// </summary>
        private void mediaPlayer_MediaEnded (object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop ();
        }

        /// <summary>
        /// Initialise UI elements based on current media item
        /// </summary>
        private void mediaPlayer_MediaOpened (object sender, RoutedEventArgs e)
        {
            sliderTime.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            sliderTime.IsEnabled = mediaPlayer.IsLoaded;
            sliderVolume.IsEnabled = mediaPlayer.IsLoaded;
        }

        /// <summary>
        /// stop the media playing
        /// </summary>
        private void btnStop_Click (object sender, RoutedEventArgs e)
        {
            // The Stop method stops and resets the media to be played from
            // the beginning.
            mediaPlayer.Stop ();
        }

        /// <summary>
        /// pause the media playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPause_Click (object sender, RoutedEventArgs e)
        {
            // The Pause method pauses the media if it is currently running.
            // The Play method can be used to resume.
            mediaPlayer.Pause ();
        }

        /// <summary>
        /// play the media
        /// </summary>
        private void btnPlay_Click (object sender, RoutedEventArgs e)
        {
            // The Play method will begin the media if it is not currently active or 
            // resume media if it is paused. This has no effect if the media is
            // already running.
            if (imagePlayer.Visibility == Visibility.Visible)
            {
                imagePlayer.Visibility = Visibility.Collapsed;
                mediaPlayer.Visibility = Visibility.Visible;
            }

            mediaPlayer.Play ();
            mediaPlayer.Volume = (double) sliderVolume.Value;
        }

        /// <summary>
        /// seek media to position (x)
        /// </summary>
        private void SeekToMediaPosition (object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            int SliderValue = (int) sliderTime.Value;
            // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan (0, 0, 0, 0, SliderValue);
            mediaPlayer.Position = ts;
        }

        /// <summary>
        /// change media volume to position (x)
        /// </summary>
        private void ChangeMediaVolume (object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            mediaPlayer.Volume = (double) sliderVolume.Value;
        }


        #endregion

        private void UserControl_Loaded (object sender, RoutedEventArgs e)
        {
            BitmapImage bitmapImage = new BitmapImage (new Uri (ImageFile));
            imagePlayer.Source = bitmapImage;
            mediaPlayer.Source = null;
            mediaPlayer.Source = new Uri (MovieFile);
            mediaPlayerBorder.Visibility = Visibility.Visible;
            //mediaPlayer.Play ();
            mediaPlayer.Volume = (double) sliderVolume.Value;
        }
    }
}