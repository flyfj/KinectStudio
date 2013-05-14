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
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectMotionAnalyzer.Processors;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;

namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic for FMSProcessorView.xaml
    /// </summary>
    public partial class FMSProcessorView : UserControl
    {
        private readonly KinectSensorChooser sensorChooser = null;
        private KinectSensor kinect_sensor = null;
        private KinectDataManager query_kinect_data_manager = null;

        private bool ifDoSmoothing = true;
        private bool isQueryCapturing = false;

        private int MAX_ALLOW_FRAME = 500;
        private List<Skeleton> query_skeleton_rec_buffer = null; // record skeleton data
        private List<byte[]> query_color_frame_rec_buffer = null; // record video frames


        public FMSProcessorView(KinectSensorChooser chooser)
        {
            InitializeComponent();

            sensorChooser = chooser;

            query_skeleton_rec_buffer = new List<Skeleton>();
            query_color_frame_rec_buffer = new List<byte[]>();
        }

        private void mainGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.mainGrid.Visibility == Visibility.Hidden)
            {
                var parent = (Panel)this.Parent;
                parent.Children.Remove(this);

                sensorChooser.Start();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.controlKinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);

            kinect_sensor = sensorChooser.Kinect;

            // enable data stream
            if (kinect_sensor != null)
            {
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

                kinect_sensor.Start();
            }
            
        }

        void kinect_allframes_ready(object sender, AllFramesReadyEventArgs e)
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
                        if (query_skeleton_rec_buffer.Count == MAX_ALLOW_FRAME)
                            query_skeleton_rec_buffer.RemoveAt(0);

                        // just add first tracked skeleton, assume only one person is present
                        query_skeleton_rec_buffer.Add(tracked_skeleton);

                        ifAddSkeleton = true;
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

        private void exitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor != null && kinect_sensor.IsRunning)
                kinect_sensor.Stop();

            query_color_frame_rec_buffer.Clear();
            query_skeleton_rec_buffer.Clear();
        }
    }
}
