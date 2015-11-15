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
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;


namespace WPF_Media_Player
{
    /// <summary>
    /// prvoides a media player with a track list and volume controls and rating system
    /// </summary>
    public partial class ucMediaPlayer : System.Windows.Controls.UserControl
    {
        private DispatcherTimer timer;
        private double currentposition = 0;
        private bool bDragging = false;
        private bool bTimerChangedValue = false;

        public ucMediaPlayer ()
        {
            InitializeComponent ();
            IsPlaying (false);
            timer = new DispatcherTimer ();
            timer.Interval = TimeSpan.FromMilliseconds (2000);
            timer.Tick += new EventHandler (timer_Tick);
            sliderTime.IsEnabled = false;
            sliderVolume.IsEnabled = false;
        }

        /***********************************************
         *
         * timer_Tick
         *
         ***********************************************/

        private void timer_Tick (object sender, EventArgs e)
        {
            if (!bDragging)
            {
                bTimerChangedValue = true;
                sliderTime.Value = mediaPlayer.Position.TotalSeconds;
                bTimerChangedValue = false;
                currentposition = sliderTime.Value;
            }
        }

        public string ImageFile { get; set; }
        public string MovieFile { get; set; }

        /***********************************************
        *
        * Is Playing
        *
        ***********************************************/

        private void IsPlaying (bool bValue)
        {
            btnStop.IsEnabled = bValue;
            //btnMoveBackward.IsEnabled = bValue;
            //btnMoveForward.IsEnabled = bValue;
            //btnPlay.IsEnabled = bValue;
            //btnScreenShot.IsEnabled = bValue;
            //seekBar.IsEnabled = bValue;
        }
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
            sliderTime.IsEnabled = mediaPlayer.IsLoaded;
            sliderVolume.IsEnabled = mediaPlayer.IsLoaded;

            if (mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan ts = mediaPlayer.NaturalDuration.TimeSpan;
                sliderTime.Maximum = ts.TotalSeconds;
                sliderTime.SmallChange = 1;
                sliderTime.LargeChange = Math.Min (10, ts.Seconds / 10);
            }
            timer.Start ();
        }

        /// <summary>
        /// stop the media playing
        /// </summary>
        private void btnStop_Click (object sender, RoutedEventArgs e)
        {
            // The Stop method stops and resets the media to be played from
            // the beginning.
            IsPlaying (false);
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
            IsPlaying (false);
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

            IsPlaying (true);
            mediaPlayer.Play ();
            mediaPlayer.Volume = (double) sliderVolume.Value;
        }

         /// <summary>
        /// change media volume to position (x)
        /// </summary>
        private void ChangeMediaVolume (object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            mediaPlayer.Volume = (double) sliderVolume.Value;
        }

        /**************************************************
        *
        * seekBar_DragStarted
        *
        **************************************************/

        private void seekBar_DragStarted (object sender, DragStartedEventArgs e)
        {
            bDragging = true;
        }

        /**************************************************
        *
        * seekBar_DragCompleted
        *
        **************************************************/

        private void seekBar_DragCompleted (object sender, DragCompletedEventArgs e)
        {
            bDragging = false;
            mediaPlayer.Position = TimeSpan.FromSeconds (sliderTime.Value);
        }

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

        private void sliderTime_ValueChanged (object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!bTimerChangedValue && !bDragging)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds (sliderTime.Value);
            }
        }
    }
}