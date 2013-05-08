//------------------------------------------------------------------------------
// <copyright file="VideoPlayer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Controls
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;

    /// <summary>
    /// A video player which automatically loops and can be controlled through a single Boolean dependency property.
    /// </summary>
    [TemplatePart(Name = MediaElementSitePartName, Type = typeof(MediaElement))]
    [TemplatePart(Name = VideoProgressBarSitePartName, Type = typeof(MediaElement))]
    [TemplatePart(Name = DurationSitePartName, Type = typeof(MediaElement))]
    [TemplatePart(Name = CurrentProgressSitePartName, Type = typeof(MediaElement))]
    public class VideoPlayer : Control
    {
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register("IsPlaying", typeof(bool), typeof(VideoPlayer), new UIPropertyMetadata(false, OnIsPlayingChanged));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(VideoPlayer), new UIPropertyMetadata(null, OnSourceChanged));

        public static readonly DependencyProperty ShowProgressBarProperty = DependencyProperty.Register(
            "ShowProgressBar", typeof(bool), typeof(VideoPlayer), new PropertyMetadata(true));

        /// <summary>
        /// Template part name for the MediaElement responsible for displaying the video.
        /// </summary>
        private const string MediaElementSitePartName = "MediaElementSite";

        /// <summary>
        /// Template part name for the ProgressBar responsible for displaying the playback progress.
        /// </summary>
        private const string VideoProgressBarSitePartName = "VideoProgressBarSite";

        /// <summary>
        /// Template part name for the TextBlock responsible for displaying the duration of the video.
        /// </summary>
        private const string DurationSitePartName = "DurationSite";

        /// <summary>
        /// Template part name for the TextBlock responsible for displaying the current progress of the video.
        /// </summary>
        private const string CurrentProgressSitePartName = "CurrentProgressSite";

        /// <summary>
        /// MediaElement responsible for displaying the video.
        /// </summary>
        private MediaElement mediaElement;

        private ProgressBar progressBar;
        private TextBlock currentProgressTextBlock;
        private TextBlock durationTextBlock;

        private DispatcherTimer progressTimer;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "DefaultStyleKey.OverrideMetadata must be called from a static constructor")]
        static VideoPlayer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer), new FrameworkPropertyMetadata(typeof(VideoPlayer)));
        }

        public VideoPlayer()
        {
            this.progressTimer = new DispatcherTimer();
            this.progressTimer.Interval = TimeSpan.FromMilliseconds(100.0);
            this.progressTimer.Tick += this.OnProgressUpdateTick;
        }

        /// <summary>
        /// Event that is signaled when the end of the video is reached.
        /// </summary>
        public event EventHandler VideoEnded;

        /// <summary>
        /// Gets or sets whether the video is currently playing or is paused
        /// </summary>
        public bool IsPlaying
        {
            get { return (bool)this.GetValue(IsPlayingProperty); }
            set { this.SetValue(IsPlayingProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the video is currently playing or is paused
        /// </summary>
        public bool ShowProgressBar
        {
            get { return (bool)this.GetValue(ShowProgressBarProperty); }
            set { this.SetValue(ShowProgressBarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Uri pointing to the video. This cannot be a resource Uri.
        /// </summary>
        public Uri Source
        {
            get { return (Uri)this.GetValue(SourceProperty); }
            set { this.SetValue(SourceProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            if (null != this.mediaElement)
            {
                this.mediaElement.MediaOpened -= this.OnVideoOpened;
                this.mediaElement.MediaEnded -= this.OnVideoEnded;
            }

            base.OnApplyTemplate();

            this.mediaElement = this.GetTemplateChild(MediaElementSitePartName) as MediaElement;
            this.progressBar = this.GetTemplateChild(VideoProgressBarSitePartName) as ProgressBar;
            this.durationTextBlock = this.GetTemplateChild(DurationSitePartName) as TextBlock;
            this.currentProgressTextBlock = this.GetTemplateChild(CurrentProgressSitePartName) as TextBlock;

            if (null != this.mediaElement)
            {
                this.mediaElement.MediaEnded += this.OnVideoEnded;
                this.mediaElement.MediaOpened += this.OnVideoOpened;
            }
        }

        private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            VideoPlayer videoPlayer = obj as VideoPlayer;
            if (null != videoPlayer.mediaElement)
            {
                videoPlayer.mediaElement.Source = (Uri)e.NewValue;
            }
        }

        private static void OnIsPlayingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            VideoPlayer videoPlayer = obj as VideoPlayer;
            if (null != videoPlayer.mediaElement)
            {
                if (e.NewValue is bool && (bool)e.NewValue)
                {
                    videoPlayer.progressTimer.Start();
                    videoPlayer.mediaElement.Play();
                }
                else
                {
                    videoPlayer.progressTimer.Stop();
                    videoPlayer.mediaElement.Pause();
                }
            }
        }

        /// <summary>
        /// Internal event handler that resets the position of the video to the start and invokes the VideoEnded event
        /// </summary>
        private void OnVideoEnded(object sender, RoutedEventArgs e)
        {
            this.mediaElement.Position = TimeSpan.Zero;

            EventHandler handler = this.VideoEnded;
            if (null != handler)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnVideoOpened(object sender, RoutedEventArgs e)
        {
            if (null != this.progressBar)
            {
                this.progressBar.Maximum = this.mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            }

            if (null != this.durationTextBlock)
            {
                this.durationTextBlock.Text = this.mediaElement.NaturalDuration.TimeSpan.ToString(@"m\:ss", CultureInfo.InvariantCulture);
            }
        }

        private void OnProgressUpdateTick(object sender, EventArgs e)
        {
            if (null != this.progressBar)
            {
                this.progressBar.Value = this.mediaElement.Position.TotalMilliseconds;
            }

            if (null != this.currentProgressTextBlock)
            {
                this.currentProgressTextBlock.Text = this.mediaElement.Position.ToString(@"m\:ss", CultureInfo.InvariantCulture);
            }
        }
    }
}