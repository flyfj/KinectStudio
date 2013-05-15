using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;


namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic for HomeScreenView.xaml
    /// </summary>
    public partial class HomeScreenView : UserControl
    {

        private readonly KinectSensorChooser sensorChooser = null;
        private readonly MainUserWindow parentWindow = null;

        public static readonly DependencyProperty PageLeftEnabledProperty = DependencyProperty.Register(
            "PageLeftEnabled", typeof(bool), typeof(HomeScreenView), new PropertyMetadata(false));

        public static readonly DependencyProperty PageRightEnabledProperty = DependencyProperty.Register(
            "PageRightEnabled", typeof(bool), typeof(HomeScreenView), new PropertyMetadata(false));

        private const double ScrollErrorMargin = 0.001;

        private const int PixelScrollByAmount = 20;


        public HomeScreenView(KinectSensorChooser chooser, MainUserWindow parentWin)
        {
            InitializeComponent();

            sensorChooser = chooser;

            parentWindow = parentWin;

            // Add in display content
            for (var index = 5; index < 100; ++index)
            {
                var button = new KinectTileButton
                {
                    Label = (index + 1).ToString(CultureInfo.CurrentCulture),
                    Style = FindResource("KinectTileButtonStyle") as Style
                };
                button.Margin = new System.Windows.Thickness(10);
                this.wrapPanel.Children.Add(button);
            }

            // Bind listener to scroll viewer scroll position change, and check scroll viewer position
            this.UpdatePagingButtonState();
            scrollViewer.ScrollChanged += (o, e) => this.UpdatePagingButtonState();

        }

        /// <summary>
        /// CLR Property Wrappers for PageLeftEnabledProperty
        /// </summary>
        public bool PageLeftEnabled
        {
            get
            {
                return (bool)GetValue(PageLeftEnabledProperty);
            }

            set
            {
                this.SetValue(PageLeftEnabledProperty, value);
            }
        }

        /// <summary>
        /// CLR Property Wrappers for PageRightEnabledProperty
        /// </summary>
        public bool PageRightEnabled
        {
            get
            {
                return (bool)GetValue(PageRightEnabledProperty);
            }

            set
            {
                this.SetValue(PageRightEnabledProperty, value);
            }
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private static void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try
                    {
                        //args.NewSensor.DepthStream.Range = DepthRange.Near;
                        //args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        /// <summary>
        /// Handle paging right (next button).
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void PageRightButtonClick(object sender, RoutedEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + PixelScrollByAmount);
        }

        /// <summary>
        /// Handle paging left (previous button).
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void PageLeftButtonClick(object sender, RoutedEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - PixelScrollByAmount);
        }

        /// <summary>
        /// Change button state depending on scroll viewer position
        /// </summary>
        private void UpdatePagingButtonState()
        {
            this.PageLeftEnabled = scrollViewer.HorizontalOffset > ScrollErrorMargin;
            this.PageRightEnabled = scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth - ScrollErrorMargin;
        }

        private void KinectTileButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (KinectTileButton)e.OriginalSource;
            string caption = button.Label as string;
            if (caption == "Any Motion")
            {
                parentWindow.kinectRegion.IsEnabled = false;
                ActionMatcherView matcherView = new ActionMatcherView(parentWindow);
                parentWindow.globalGrid.Children.Add(matcherView);
            }
            else if (caption == "FMS")
            {
                FMSSelectorView fmsView = new FMSSelectorView(sensorChooser, parentWindow);
                (this.Parent as Panel).Children.Add(fmsView);
            }
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            parentWindow.Close();
        }
    }
}
