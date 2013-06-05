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
using KinectMotionAnalyzer.DataModel;
using KinectMotionAnalyzer.Processors;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;

namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic for ActionMatcherView.xaml
    /// </summary>
    public partial class ActionMatcherView : UserControl
    {

        private KinectSensorChooser sensorChooser = null;
        private readonly MainUserWindow parentWindow = null;
        private KinectSensor kinect_sensor = null;
        private KinectDataManager query_kinect_data_manager = null;
        private ActionRecognizer actionRecognizer = null;

        private bool ifDoSmoothing = true;
        private bool isQueryCapturing = false;
        private bool ifStartedTracking = false; // sign to indicate if the tracking has started

        private int MAX_ALLOW_FRAME = 700;
        private double ACTION_RECOGNITION_TH = 150;
        private List<Skeleton> query_skeleton_rec_buffer = null; // record skeleton data
        private List<byte[]> query_color_frame_rec_buffer = null; // record video frames
        private List<Skeleton> target_skeleton_rec_buffer = null;


        public ActionMatcherView(MainUserWindow parentWin)
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.controlKinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);

            kinect_sensor = sensorChooser.Kinect;

            // create data
            query_color_frame_rec_buffer = new List<byte[]>();
            query_skeleton_rec_buffer = new List<Skeleton>();
            target_skeleton_rec_buffer = new List<Skeleton>();

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
                if (query_kinect_data_manager == null)
                    query_kinect_data_manager = new KinectDataManager(ref kinect_sensor);

                // initialize action recognizer
                if (actionRecognizer == null)
                    actionRecognizer = new ActionRecognizer();

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

                this.ifStartedTracking = false;

                // set ui
                this.controlKinectRegion.IsEnabled = false;
                this.controlKinectRegion.Visibility = Visibility.Hidden;

                // give prompt
                this.infoTextBlock.Text = "Processing activated.\nLeave the screen to enable interaction.";

                // load template action
                //LoadPrerecordAction();
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
                if (target_skeleton_rec_buffer != null)
                    target_skeleton_rec_buffer.Clear();

                if (query_kinect_data_manager != null)
                    query_kinect_data_manager.UpdateSkeletonData(new Skeleton[0]);

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

                        if (query_skeleton_rec_buffer.Count >= target_skeleton_rec_buffer.Count)
                        {
                            double dist = ComputeActionSimilarity();
                            if (dist < ACTION_RECOGNITION_TH)
                            {
                                infoTextBlock.Text = "Action Detected: " + dist;
                            }
                            else
                                infoTextBlock.Text = "Action not detected: " + dist;
                            //Console.WriteLine(dist);
                        }
                    }
                    else
                    {
                        // has started but lose tracking
                        if (ifStartedTracking)
                        {
                            // stop processing and start interaction
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
                        //if (query_color_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                        //    query_color_frame_rec_buffer.RemoveAt(0);

                        //query_color_frame_rec_buffer.Add(colorData);
                    }
                }

                query_kinect_data_manager.UpdateColorData(frame);
 
            }
            #endregion

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

        private void LoadPrerecordAction()
        {
            using (MotionDBContext dbcontext = new MotionDBContext())
            {
                //MessageBox.Show(dbcontext.Database.Connection.ConnectionString);

                // retrieve action from database
                var query = from ac in dbcontext.Actions.Include("Skeletons.JointsData")
                            where ac.Id == 3
                            select ac;

                if (query != null)
                {
                    List<byte[]> color_frame_rec_buffer = null;
                    List<DepthImagePixel[]> depth_frame_rec_buffer = null;
                    foreach (var q in query)
                    {
                        KinectAction sel_action = q as KinectAction;
                        if (!Tools.ConvertFromKinectAction(
                                sel_action,
                                out color_frame_rec_buffer,
                                out depth_frame_rec_buffer,
                                out target_skeleton_rec_buffer))
                        {
                            MessageBox.Show("Fail to load database action.");
                            return;
                        }
                        else
                        {
                            infoTextBlock.Text = "Action loaded.";
                        }
                        break;
                    }
                }
            }
        }

        private double ComputeActionSimilarity()
        {
            if (query_skeleton_rec_buffer.Count <= 0 || target_skeleton_rec_buffer.Count <= 0)
                return 100000000;

            // do matching using dtw
            // try 1/2 length, same length, 2 length to see which one is most similar
            KinectMotionAnalyzer.Processors.Action queryAction = new KinectMotionAnalyzer.Processors.Action();
            queryAction.name = "Query";
            KinectMotionAnalyzer.Processors.Action targetAction = new KinectMotionAnalyzer.Processors.Action();
            targetAction.name = "Target";
            targetAction.data = target_skeleton_rec_buffer;

            double minDist = double.PositiveInfinity;
            int bestType = 0;
            // 1/2 length
            //queryAction.data = query_skeleton_rec_buffer.GetRange(
            //    query_skeleton_rec_buffer.Count - target_skeleton_rec_buffer.Count / 2, 
            //    target_skeleton_rec_buffer.Count / 2);

            //double dist = actionRecognizer.ActionSimilarity(queryAction, targetAction, 0);
            //if (dist < minDist)
            //{
            //    minDist = dist;
            //    bestType = 0;
            //}
            // same length
            queryAction.data = query_skeleton_rec_buffer.GetRange(
                query_skeleton_rec_buffer.Count - target_skeleton_rec_buffer.Count,
                target_skeleton_rec_buffer.Count);
            double dist = actionRecognizer.ActionSimilarity(queryAction, targetAction, 0);
            if (dist < minDist)
            {
                minDist = dist;
                bestType = 1;
            }
            // 2 length
            //queryAction.data = query_skeleton_rec_buffer.GetRange(
            //    query_skeleton_rec_buffer.Count - target_skeleton_rec_buffer.Count * 2,
            //    target_skeleton_rec_buffer.Count * 2);
            //dist = actionRecognizer.ActionSimilarity(queryAction, targetAction, 0);
            //if (dist < minDist)
            //{
            //    minDist = dist;
            //    bestType = 2;
            //}

            return dist;
        }
    }
}
