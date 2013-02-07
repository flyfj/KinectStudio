using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Win32;


namespace KinectMotionAnalyzer
{ 
    
    using KinectMotionAnalyzer.Processors;

    /// <summary>
    /// Interaction logic for GestureRecognizerWindow.xaml
    /// </summary>
    public partial class GestureRecognizerWindow : Window
    {

        private KinectDataManager kinect_data_manager;
        private KinectSensor kinect_sensor;

        bool isReplay = false;
        
        // record params
        private int frame_id = 0;
        Dictionary<int, Skeleton[]> gesture_data = new Dictionary<int, Skeleton[]>();


        public GestureRecognizerWindow()
        {
            InitializeComponent();

            if (!InitKinect())
            {
                statusbarLabel.Content = "Kinect not connected";
                MessageBox.Show("Kinect not found.");
            }
            else
                statusbarLabel.Content = "Kinect initialized";
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


            if (kinect_sensor != null)
                kinect_data_manager = new KinectDataManager(ref kinect_sensor);

            // enable data stream
            if (kinect_sensor != null)
            {
                // initialize stream
                kinect_sensor.SkeletonStream.Enable();

                // set source (must after source has been initialized otherwise it's null forever)
                skeleton_disp_img.Source = kinect_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;
            }
            else
                return false;


            return true;
        }


        void kinect_skeletonframe_ready(object sender, SkeletonFrameReadyEventArgs e)
        {

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                // get skeleton data
                Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                // if capturing, add to gesture data
                if (gestureCaptureBtn.Content.ToString() == "Stop Capture")
                {
                    gesture_data.Add(frame_id, skeletons);
                    frame_id++;
                }

                kinect_data_manager.UpdateSkeletonData(skeletons);
            }
        }

        private void kinectRunBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!kinect_sensor.IsRunning)
            {
                kinect_sensor.Start();
                kinectRunBtn.Content = "Stop";
                gestureCaptureBtn.IsEnabled = true;
                gestureReplayBtn.IsEnabled = false;
            }
            else
            {
                kinect_sensor.Stop();
                kinectRunBtn.Content = "Run";
                gestureCaptureBtn.IsEnabled = false;
                gestureReplayBtn.IsEnabled = true;
            }
        }

        private void gestureCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (gestureCaptureBtn.Content.ToString() == "Capture")
            {
                // reset
                frame_id = 0;
                gesture_data.Clear();
                gestureCaptureBtn.Content = "Stop Capture";
            }
            else
            {
                // save to file
                string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

                string myPhotos = "D:"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                string skeletonpath = myPhotos + "\\Kinect_skeleton_" + time + ".xml";

                KinectRecorder.WriteToSkeletonFile(skeletonpath, gesture_data);

                gesture_data.Clear();
                frame_id = 0;

                statusbarLabel.Content = "Save skeletons to file: " + skeletonpath;
            }
            
        }

        private void skeletonVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (!kinect_sensor.IsRunning && isReplay && gesture_data.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)skeletonVideoSlider.Value;
                if (gesture_data[cur_frame_id] != null)
                {
                    kinect_data_manager.UpdateSkeletonData(gesture_data[cur_frame_id]);
                }

                // update label
                skeletonSliderLabel.Content = skeletonVideoSlider.Value.ToString();
            }
        }

        private void gestureReplayBtn_Click(object sender, RoutedEventArgs e)
        {

            if (kinect_sensor.IsRunning)
                return;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".xml";
            dialog.FileName = "Skeleton";
            dialog.Filter = "Skeleton data file (.xml)|*.xml";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                // test: read skeleton data and display
                gesture_data = KinectRecorder.ReadFromSkeletonFile(filename);

                int min_frame_id = gesture_data.Keys.Min();
                int max_frame_id = gesture_data.Keys.Max();

                skeletonVideoSlider.IsEnabled = true;
                skeletonVideoSlider.Minimum = min_frame_id;
                skeletonVideoSlider.Maximum = max_frame_id;
                skeletonVideoSlider.Value = min_frame_id;
                skeletonSliderLabel.Content = min_frame_id.ToString();

                kinect_data_manager.UpdateSkeletonData(gesture_data[min_frame_id]);

                isReplay = true;
            }

        }

        
    }
}
