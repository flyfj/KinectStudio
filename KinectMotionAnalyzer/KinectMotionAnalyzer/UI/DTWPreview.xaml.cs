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
using Microsoft.Kinect;
using KinectMotionAnalyzer.Processors;

namespace KinectMotionAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for DTWPreview.xaml
    /// </summary>
    public partial class DTWPreview : Window
    {
        // tools
        private KinectDataManager query_kinect_data_manager;
        private KinectDataManager target_kinect_data_manager;
        private KinectSensor kinect_sensor;

        // sign
        bool ifDoSmoothing = true;
        bool isQueryReplaying = false;
        bool isTargetReplaying = false;
        bool isQueryCapturing = false;
        bool isTargetCapturing = false;

        // record params
        private int MAX_ALLOW_FRAME = 500;  // no more than this number for color and skeleton to avoid memory issue
        List<Skeleton> query_skeleton_rec_buffer = null; // record skeleton data
        List<byte[]> query_color_frame_rec_buffer = null; // record video frames
        List<Skeleton> target_skeleton_rec_buffer = null; // record skeleton data
        List<byte[]> target_color_frame_rec_buffer = null; // record video frames

        public DTWPreview()
        {
            InitializeComponent();
        }


        /// <summary>
        /// initialize kinect sensor and init data members
        /// </summary>
        private bool InitKinect()
        {
            // enumerate and fetch an available sensor
            foreach (var potentialsensor in KinectSensor.KinectSensors)
            {
                if (potentialsensor.Status == KinectStatus.Connected)
                {
                    kinect_sensor = potentialsensor;
                    break;
                }
            }

            // enable data stream
            if (kinect_sensor != null)
            {
                // initialize data manager
                query_kinect_data_manager = new KinectDataManager(ref kinect_sensor);
                target_kinect_data_manager = new KinectDataManager(ref kinect_sensor);

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

                // set target source
                target_kinect_data_manager.ColorStreamBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);
                color_target_img.Source = target_kinect_data_manager.ColorStreamBitmap;
                ske_target_img.Source = target_kinect_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.AllFramesReady += kinect_allframes_ready;
            }
            else
            {
                return false;
            }

            return true;
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

                    query_kinect_data_manager.UpdateSkeletonData(tracked_skeleton);
                }

                if (isTargetCapturing)
                {
                    if (tracked_skeleton != null)
                    {
                        if (target_skeleton_rec_buffer.Count == MAX_ALLOW_FRAME)
                            target_skeleton_rec_buffer.RemoveAt(0);

                        // just add first tracked skeleton, assume only one person is present
                        target_skeleton_rec_buffer.Add(tracked_skeleton);

                        ifAddSkeleton = true;
                    }

                    target_kinect_data_manager.UpdateSkeletonData(tracked_skeleton);
                }
            }
#endregion
            
#region handle color frame
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                byte[] colorData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(colorData);

                if (isQueryCapturing && ifAddSkeleton)
                {
                    // remove oldest frame
                    if (query_color_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                        query_color_frame_rec_buffer.RemoveAt(0);

                    query_color_frame_rec_buffer.Add(colorData);

                    query_kinect_data_manager.UpdateColorData(frame);
                }

                if (this.isTargetCapturing && ifAddSkeleton)
                {
                    // remove oldest frame
                    if (target_color_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                        target_color_frame_rec_buffer.RemoveAt(0);

                    target_color_frame_rec_buffer.Add(colorData);

                    target_kinect_data_manager.UpdateColorData(frame);
                }
            }
#endregion
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // do initialization here
            query_skeleton_rec_buffer = new List<Skeleton>();
            query_color_frame_rec_buffer = new List<byte[]>();
            target_color_frame_rec_buffer = new List<byte[]>();
            target_skeleton_rec_buffer = new List<Skeleton>();

            // init kinect
            if (!InitKinect())
            {
                statusbarLabel.Content = "Kinect not connected";
                MessageBox.Show("Kinect not found.");
            }
            else
                statusbarLabel.Content = "Kinect initialized";

            DeactivateQueryReplay();
            DeactivateTargetReplay();
        }

#region replay management

        private void ActivateQueryReplay(List<byte[]> color_frame_rec_buffer, List<Skeleton> skeleton_rec_buffer)
        {
            if (this.query_color_frame_rec_buffer == null || this.query_color_frame_rec_buffer.Count == 0 ||
                this.query_skeleton_rec_buffer == null || this.query_skeleton_rec_buffer.Count == 0)
            {
                MessageBox.Show("Replay action is empty.");
                return;
            }

            int min_frame_id = 0;
            int max_frame_id = this.query_color_frame_rec_buffer.Count - 1;
            if (query_skeleton_rec_buffer != null)
                max_frame_id = Math.Min(max_frame_id, this.query_skeleton_rec_buffer.Count - 1);

            queryVideoSlider.IsEnabled = true;
            queryVideoSlider.Minimum = min_frame_id;
            queryVideoSlider.Maximum = max_frame_id;
            queryVideoSlider.SelectionStart = min_frame_id;
            queryVideoSlider.SelectionEnd = max_frame_id;
            queryVideoSlider.Value = min_frame_id;
            queryVideoSliderLabel.Content = min_frame_id.ToString();

            query_setStartBtn.IsEnabled = true;
            query_startLabel.Content = min_frame_id.ToString();
            query_endLabel.Content = max_frame_id.ToString();

            isQueryReplaying = true;

            // update view
            query_kinect_data_manager.UpdateColorData(color_frame_rec_buffer[min_frame_id], 640, 480);
            query_kinect_data_manager.UpdateSkeletonData(skeleton_rec_buffer[min_frame_id]);
        }

        private void ActivateTargetReplay(List<byte[]> color_frame_rec_buffer, List<Skeleton> skeleton_rec_buffer)
        {
            if (this.target_color_frame_rec_buffer == null || this.target_color_frame_rec_buffer.Count == 0 ||
                this.target_skeleton_rec_buffer == null || this.target_skeleton_rec_buffer.Count == 0)
            {
                MessageBox.Show("Replay action is empty.");
                return;
            }

            int min_frame_id = 0;
            int max_frame_id = this.target_color_frame_rec_buffer.Count - 1;
            if (target_skeleton_rec_buffer != null)
                max_frame_id = Math.Min(max_frame_id, this.target_skeleton_rec_buffer.Count - 1);

            targetVideoSlider.IsEnabled = true;
            targetVideoSlider.Minimum = min_frame_id;
            targetVideoSlider.Maximum = max_frame_id;
            targetVideoSlider.SelectionStart = min_frame_id;
            targetVideoSlider.SelectionEnd = max_frame_id;
            targetVideoSlider.Value = min_frame_id;
            targetVideoSliderLabel.Content = min_frame_id.ToString();

            target_setStartBtn.IsEnabled = true;
            target_startLabel.Content = min_frame_id.ToString();
            target_endLabel.Content = max_frame_id.ToString();

            this.isTargetReplaying = true;

            // update view
            target_kinect_data_manager.UpdateColorData(target_color_frame_rec_buffer[min_frame_id], 640, 480);
            target_kinect_data_manager.UpdateSkeletonData(this.target_skeleton_rec_buffer[min_frame_id]);
        }

        private void DeactivateQueryReplay()
        {
            // clear all
            queryVideoSlider.IsEnabled = false;
            queryVideoSlider.SelectionStart = 0;
            queryVideoSlider.SelectionEnd = 0;
            queryVideoSlider.Minimum = 0;
            queryVideoSlider.Maximum = 0;
            queryVideoSlider.Value = 0;
            queryVideoSliderLabel.Content = "0";

            query_setStartBtn.IsEnabled = false;
            query_startLabel.Content = "0";
            query_setEndBtn.IsEnabled = false;
            query_endLabel.Content = "0";

            this.isQueryReplaying = false;
        }

        private void DeactivateTargetReplay()
        {
            // clear all
            targetVideoSlider.IsEnabled = false;
            targetVideoSlider.SelectionStart = 0;
            targetVideoSlider.SelectionEnd = 0;
            targetVideoSlider.Minimum = 0;
            targetVideoSlider.Maximum = 0;
            targetVideoSlider.Value = 0;
            targetVideoSliderLabel.Content = "0";

            target_setStartBtn.IsEnabled = false;
            target_startLabel.Content = "0";
            target_setEndBtn.IsEnabled = false;
            target_endLabel.Content = "0";

            this.isTargetReplaying = false;
        }

        private void queryVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (this.isQueryReplaying && query_color_frame_rec_buffer.Count > 0 && query_skeleton_rec_buffer.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)queryVideoSlider.Value;
                if (query_skeleton_rec_buffer.Count > cur_frame_id && query_color_frame_rec_buffer.Count > cur_frame_id)
                {
                    query_kinect_data_manager.UpdateColorData(query_color_frame_rec_buffer[cur_frame_id], 640, 480);
                    query_kinect_data_manager.UpdateSkeletonData(query_skeleton_rec_buffer[cur_frame_id]);

                    // update label
                    queryVideoSliderLabel.Content = queryVideoSlider.Value.ToString();
                }
            }
        }

        private void targetVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (this.isTargetReplaying && target_color_frame_rec_buffer.Count > 0 && target_skeleton_rec_buffer.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)targetVideoSlider.Value;
                if (target_skeleton_rec_buffer.Count > cur_frame_id && target_color_frame_rec_buffer.Count > cur_frame_id)
                {
                    target_kinect_data_manager.UpdateColorData(target_color_frame_rec_buffer[cur_frame_id], 640, 480);
                    target_kinect_data_manager.UpdateSkeletonData(target_skeleton_rec_buffer[cur_frame_id]);

                    // update label
                    targetVideoSliderLabel.Content = targetVideoSlider.Value.ToString();
                }
            }
        }

        private void query_setStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!queryVideoSlider.IsSelectionRangeEnabled)
                queryVideoSlider.IsSelectionRangeEnabled = true;

            queryVideoSlider.SelectionStart = queryVideoSlider.Value;
            query_startLabel.Content = queryVideoSlider.Value.ToString();

            query_setEndBtn.IsEnabled = true;
        }

        private void query_setEndBtn_Click(object sender, RoutedEventArgs e)
        {
            if (queryVideoSlider.Value < queryVideoSlider.SelectionStart)
            {
                MessageBox.Show("End frame can't be earlier than start frame.");
                return;
            }

            queryVideoSlider.SelectionEnd = queryVideoSlider.Value;
            query_endLabel.Content = queryVideoSlider.Value;
        }

        private void target_setStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!targetVideoSlider.IsSelectionRangeEnabled)
                targetVideoSlider.IsSelectionRangeEnabled = true;

            targetVideoSlider.SelectionStart = targetVideoSlider.Value;
            target_startLabel.Content = targetVideoSlider.Value.ToString();

            target_setEndBtn.IsEnabled = true;
        }

        private void target_setEndBtn_Click(object sender, RoutedEventArgs e)
        {
            if (targetVideoSlider.Value < targetVideoSlider.SelectionStart)
            {
                MessageBox.Show("End frame can't be earlier than start frame.");
                return;
            }

            targetVideoSlider.SelectionEnd = targetVideoSlider.Value;
            target_endLabel.Content = targetVideoSlider.Value;
        }

#endregion

        private void queryCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!this.isQueryCapturing)
            {
                // reset buffer
                query_color_frame_rec_buffer.Clear();
                query_skeleton_rec_buffer.Clear();
                // set signs
                queryCaptureBtn.Content = "Stop";
                this.isQueryCapturing = true;

                // start kinect
                if (!kinect_sensor.IsRunning)
                {
                    // can't replay since share same gesture buffer
                    DeactivateQueryReplay();
                    kinect_sensor.Start();
                }
            }
            else
            {
                if (kinect_sensor == null)
                    return;

                kinect_sensor.Stop();
                this.isQueryCapturing = false;

                // prepare for replay
                this.ActivateQueryReplay(query_color_frame_rec_buffer, query_skeleton_rec_buffer);
                queryCaptureBtn.Content = "Capture";
            }
        }

        private void targetCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!this.isTargetCapturing)
            {
                // reset buffer
                target_color_frame_rec_buffer.Clear();
                target_skeleton_rec_buffer.Clear();
                // set signs
                targetCaptureBtn.Content = "Stop";
                this.isTargetCapturing = true;

                // start kinect
                if (!kinect_sensor.IsRunning)
                {
                    // can't replay since share same gesture buffer
                    DeactivateTargetReplay();
                    kinect_sensor.Start();
                }
            }
            else
            {
                if (kinect_sensor == null)
                    return;

                kinect_sensor.Stop();
                this.isTargetCapturing = false;

                // prepare for replay
                this.ActivateTargetReplay(target_color_frame_rec_buffer, target_skeleton_rec_buffer);
                targetCaptureBtn.Content = "Capture";
            }
        }

        

    }
}
