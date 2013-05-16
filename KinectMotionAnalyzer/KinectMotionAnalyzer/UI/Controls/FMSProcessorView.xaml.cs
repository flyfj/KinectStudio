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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectMotionAnalyzer.Processors;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;

namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic
    /// </summary>
    public partial class FMSProcessorView : UserControl
    {

        private KinectSensorChooser sensorChooser = null;
        private readonly MainUserWindow parentWindow = null;
        private KinectSensor kinect_sensor = null;
        private KinectDataManager query_kinect_data_manager = null;
        private FMSProcessor fmsProcessor = null;

        private bool ifDoSmoothing = true;
        private bool isQueryCapturing = true;
        private bool ifStartedTracking = false; // sign to indicate if the tracking has started

        private int MAX_ALLOW_FRAME = 500;
        private List<Skeleton> query_skeleton_rec_buffer = null; // record skeleton data
        private List<byte[]> query_color_frame_rec_buffer = null; // record video frames

        public FMSProcessorView(MainUserWindow parentWin)
        {
            InitializeComponent();

            parentWindow = parentWin;
            sensorChooser = parentWin.sensorChooserUi.KinectSensorChooser;
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

        private void mainGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.mainGrid.Visibility == Visibility.Hidden)
            {
                var parent = (Panel)this.Parent;
                parent.Children.Remove(this);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.controlKinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);

            kinect_sensor = sensorChooser.Kinect;

            this.query_color_frame_rec_buffer = new List<byte[]>();
            this.query_skeleton_rec_buffer = new List<Skeleton>();

            fmsProcessor = new FMSProcessor();

            PrepareKinectForInteraction();
            kinect_sensor.Start();
        }

        /// <summary>
        /// stop running kinect and set up kinect for processing use
        /// call start kinect next
        /// </summary>
        private void PrepareKinectForProcessing()
        {
            // enable data stream
            if (kinect_sensor != null)
            {
                // stop sensor
                if (kinect_sensor.IsRunning)
                    kinect_sensor.Stop();

                // initialize data manager
                query_kinect_data_manager = new KinectDataManager(ref kinect_sensor);

                // initialize stream
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                //kinect_sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                if (ifDoSmoothing)
                {
                    TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                    {
                        // Some smoothing with little latency (defaults).
                        // Only filters out small jitters.
                        // Good for gesture recognition in games.
                        smoothingParam.Smoothing = 0.5f;
                        smoothingParam.Correction = 0.5f;
                        smoothingParam.Prediction = 0.5f;
                        smoothingParam.JitterRadius = 0.05f;
                        smoothingParam.MaxDeviationRadius = 0.04f;
                    };

                    kinect_sensor.SkeletonStream.Enable(smoothingParam);
                }
                else
                    kinect_sensor.SkeletonStream.Enable();

                // set query source (must after source has been initialized otherwise it's null forever)
                query_kinect_data_manager.ColorStreamBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);
                color_query_img.Source = query_kinect_data_manager.ColorStreamBitmap;
                ske_query_img.Source = query_kinect_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.AllFramesReady += kinect_allframes_ready;

                // create data
                query_color_frame_rec_buffer.Clear();
                query_skeleton_rec_buffer.Clear();

                this.ifStartedTracking = false;

                // set ui
                this.controlKinectRegion.IsEnabled = false;
                this.controlKinectRegion.Visibility = Visibility.Hidden;

                // give prompt
                this.infoTextBlock.Text = "Processing activated.\nLeave the screen to enable interaction.";
            }
        }

        /// <summary>
        /// stop running kinect and set up kinect for interaction use
        /// just have to call kinect.start next
        /// </summary>
        private void PrepareKinectForInteraction()
        {
            if (kinect_sensor != null)
            {
                // stop sensor
                if (kinect_sensor.IsRunning)
                    kinect_sensor.Stop();

                kinect_sensor.AllFramesReady -= kinect_allframes_ready;

                // clear data
                if (query_color_frame_rec_buffer != null)
                    query_color_frame_rec_buffer.Clear();
                if (query_skeleton_rec_buffer != null)
                    query_skeleton_rec_buffer.Clear();

                // activate kinect region
                this.controlKinectRegion.IsEnabled = true;
                this.controlKinectRegion.Visibility = Visibility.Visible;

                // give prompt
                this.infoTextBlock.Text = "Interaction activated.\nPress \'Start\' to do motion.";
            }
        }

        private void kinect_allframes_ready(object sender, AllFramesReadyEventArgs e)
        {
            bool ifAddSkeleton = false;

            #region handle skeleton
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                // get skeleton data
                Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                // get first tracked skeleton
                Skeleton tracked_skeleton = null;
                foreach (Skeleton ske in skeletons)
                {
                    if (ske.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        tracked_skeleton = ske;
                        break;
                    }
                }

                // if capturing, add to gesture data
                if (isQueryCapturing)
                {
                    if (tracked_skeleton != null)
                    {
                        // not started and first successful tracking
                        if (!ifStartedTracking)
                            ifStartedTracking = true;

                        if (query_skeleton_rec_buffer.Count == MAX_ALLOW_FRAME)
                            query_skeleton_rec_buffer.RemoveAt(0);

                        // just add first tracked skeleton, assume only one person is present
                        query_skeleton_rec_buffer.Add(tracked_skeleton);

                        ifAddSkeleton = true;
                    }
                    else
                    {
                        // has started but lose tracking
                        if (ifStartedTracking)
                        {
                            // stop processing and start interaction
                            ProcessFMSTest();

                            PrepareKinectForInteraction();

                            ifStartedTracking = false;
                            kinect_sensor.Start();
                            return;
                        }
                    }
                }

                query_kinect_data_manager.UpdateSkeletonData(tracked_skeleton);

            }
            #endregion

            #region handle color frame
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                byte[] colorData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(colorData);

                if (isQueryCapturing)
                {
                    if (ifAddSkeleton)
                    {
                        // remove oldest frame
                        if (query_color_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                            query_color_frame_rec_buffer.RemoveAt(0);

                        query_color_frame_rec_buffer.Add(colorData);
                    }
                }

                query_kinect_data_manager.UpdateColorData(frame);
            }
            #endregion

        }

        private void ProcessFMSTest()
        {
            FMSTestEvaluation test_eval = fmsProcessor.EvaluateTest(query_skeleton_rec_buffer, "Hurdle Step");
            int sel_test_id = fmsProcessor.FMSName2Id("Hurdle Step");

            FMSReportWindow reportWin = new FMSReportWindow();
            reportWin.groupBox.Header = fmsProcessor.FMSTests[sel_test_id].testName;
            reportWin.ScoreLabel1.Content = "Score: " + test_eval.testScore;
            reportWin.ruleBox1.Content = fmsProcessor.FMSTests[sel_test_id].rules[0].name;
            reportWin.ruleBox1.IsChecked = (test_eval.rule_evals[0].ruleScore == 1 ? true : false);
            reportWin.ruleBox2.Content = fmsProcessor.FMSTests[sel_test_id].rules[1].name;
            reportWin.ruleBox2.IsChecked = (test_eval.rule_evals[1].ruleScore == 1 ? true : false);
            reportWin.ruleBox3.Content = fmsProcessor.FMSTests[sel_test_id].rules[2].name;
            reportWin.ruleBox3.IsChecked = (test_eval.rule_evals[2].ruleScore == 1 ? true : false);
            reportWin.Show();
        }

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            PrepareKinectForInteraction();

            // remove itself
            Panel parentContainer = this.Parent as Panel;
            if (parentContainer.Children.Count > 0)
                parentContainer.Children.RemoveAt(parentContainer.Children.Count - 1);

            parentWindow.kinectRegion.IsEnabled = true;

            kinect_sensor.Start();
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            PrepareKinectForProcessing();
            kinect_sensor.Start();
        }

    }
}
