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
        // tools
        private KinectDataManager kinect_data_manager;
        private KinectDataManager replay_data_manager;
        private KinectSensor kinect_sensor;

        // recognition
        private GestureRecognizer gesture_recognizer;
        private string GESTURE_DATABASE_DIR = "D:\\gdata\\";

        // sign
        bool isReplay = false;
        bool isRecognition = false;
        
        // record params
        private int frame_id = 0;
        Dictionary<int, Skeleton> gesture_data = new Dictionary<int, Skeleton>();
        Gesture temp_gesture = new Gesture();


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

            DeactivateReplay();
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
                kinect_data_manager = new KinectDataManager(ref kinect_sensor);
                replay_data_manager = new KinectDataManager(ref kinect_sensor);

                // initialize stream
                kinect_sensor.SkeletonStream.Enable();

                // set source (must after source has been initialized otherwise it's null forever)
                gesture_disp_img.Source = kinect_data_manager.skeletonImageSource;
                gesture_replay_img.Source = replay_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;
            }
            else
            {
                // invalidate all buttons
                kinectRunBtn.IsEnabled = false;
                gestureReplayBtn.IsEnabled = false;

                return false;
            }


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
                    // just add first tracked skeleton, assume only one person is present
                    foreach (Skeleton ske in skeletons)
                    {
                        if (ske.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            gesture_data.Add(frame_id, ske);
                            break;
                        }
                    }
                    
                    frame_id++;
                }

                if (isRecognition)
                {
                    foreach (Skeleton ske in skeletons)
                    {
                        if (ske.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            if (temp_gesture.data.Count > gesture_recognizer.gesture_max_len)
                                temp_gesture.data.RemoveAt(0);

                            temp_gesture.data.Add(ske);
                            break;
                        }
                    }

                    if(temp_gesture.data.Count >= gesture_recognizer.gesture_min_len && 
                        temp_gesture.data.Count <= gesture_recognizer.gesture_max_len)
                    {
                        // do recognition
                        float dist = gesture_recognizer.MatchToDatabase(temp_gesture);
                        recDistLabel.Content = dist.ToString();
                    }
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
                // can't replay since share same gesture buffer
                DeactivateReplay();
                gestureReplayBtn.IsEnabled = false;
                gestureCaptureBtn.IsEnabled = true;
                gestureRecognitionBtn.IsEnabled = true;

                kinect_sensor.Start();
                kinectRunBtn.Content = "Stop";
            }
            else
            {
                kinect_sensor.Stop();
                kinectRunBtn.Content = "Run";

                gestureCaptureBtn.IsEnabled = false;
                gestureReplayBtn.IsEnabled = true;
                gestureRecognitionBtn.IsEnabled = false;
                isRecognition = false;
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
                gestureRecognitionBtn.IsEnabled = false;
            }
            else
            {
                // show for replay
                if (gesture_data != null)
                {
                    ActivateReplay(gesture_data);
                    saveGestureBtn.IsEnabled = true;
                }

                gestureCaptureBtn.Content = "Capture";
                gestureRecognitionBtn.IsEnabled = true;
                
            }
            
        }

        private void saveGestureBtn_Click(object sender, RoutedEventArgs e)
        {
            // save to file
            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = "D:"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string skeletonpath = myPhotos + "\\gdata\\Kinect_skeleton_" + time + ".xml";

            // save data from start label to end label
            int start_id = int.Parse(replay_startLabel.Content.ToString());
            int end_id = int.Parse(replay_endLabel.Content.ToString());
            Dictionary<int, Skeleton> toSaveGesture = new Dictionary<int, Skeleton>();
            foreach(KeyValuePair<int, Skeleton> pair in gesture_data)
            {
                if (pair.Key >= start_id && pair.Key <= end_id)
                    toSaveGesture.Add(pair.Key, pair.Value);
            }
            KinectRecorder.WriteToSkeletonFile(skeletonpath, toSaveGesture);

            gesture_data.Clear();
            frame_id = 0;

            statusbarLabel.Content = "Save skeletons to file: " + skeletonpath;

            DeactivateReplay();
            saveGestureBtn.IsEnabled = false;
        }

        private void skeletonVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (!kinect_sensor.IsRunning && isReplay && gesture_data.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)skeletonVideoSlider.Value;
                if (gesture_data.ContainsKey(cur_frame_id))
                {
                    replay_data_manager.UpdateSkeletonData(gesture_data[cur_frame_id]);
                }

                // update label
                skeletonSliderLabel.Content = skeletonVideoSlider.Value.ToString();
            }
        }

        private void gestureReplayBtn_Click(object sender, RoutedEventArgs e)
        {

            if (kinect_sensor != null && kinect_sensor.IsRunning)
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

                statusbarLabel.Content = "Load gesture file from " + filename;

                ActivateReplay(gesture_data);

                isReplay = true;
            }

        }

        private void ActivateReplay(Dictionary<int, Skeleton> gesture)
        {
            if (gesture == null)
            {
                statusbarLabel.Content = "Replay gesture is null.";
                return;
            }

            //gesture_data = gesture;

            int min_frame_id = gesture.Keys.Min();
            int max_frame_id = gesture.Keys.Max();

            skeletonVideoSlider.IsEnabled = true;
            skeletonVideoSlider.Minimum = min_frame_id;
            skeletonVideoSlider.Maximum = max_frame_id;
            skeletonVideoSlider.Value = min_frame_id;
            skeletonSliderLabel.Content = min_frame_id.ToString();

            replay_setStartBtn.IsEnabled = true;
            replay_startLabel.Content = min_frame_id.ToString();
            replay_setEndBtn.IsEnabled = true;
            replay_endLabel.Content = max_frame_id;

            isReplay = true;

            replay_data_manager.UpdateSkeletonData(gesture[min_frame_id]);
        }

        private void DeactivateReplay()
        {
            // clear all
            skeletonVideoSlider.IsEnabled = false;
            skeletonVideoSlider.Minimum = 0;
            skeletonVideoSlider.Maximum = 0;
            skeletonVideoSlider.Value = 0;
            skeletonSliderLabel.Content = "0";

            replay_setStartBtn.IsEnabled = false;
            replay_startLabel.Content = "0";
            replay_setEndBtn.IsEnabled = false;
            replay_endLabel.Content = "0";

            isReplay = false;
        }

        private void replay_setStartBtn_Click(object sender, RoutedEventArgs e)
        {
            replay_startLabel.Content = skeletonVideoSlider.Value.ToString();
        }

        private void replay_setEndBtn_Click(object sender, RoutedEventArgs e)
        {
            if(skeletonVideoSlider.Value < double.Parse(replay_startLabel.Content.ToString()))
            {
                MessageBox.Show("End frame can't be earlier than start frame.");
                return;
            }

            replay_endLabel.Content = skeletonVideoSlider.Value;
        }

        private void gestureRecognitionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (gestureRecognitionBtn.Content.ToString() == "Start Recognition")
            {
                if (gesture_recognizer == null)
                {
                    gesture_recognizer = new GestureRecognizer();
                    if (!gesture_recognizer.LoadGestureDatabase(GESTURE_DATABASE_DIR))
                    {
                        MessageBox.Show("Fail to load gesture database for recognition.");
                        return;
                    }
                }

                isRecognition = true;
                gestureRecognitionBtn.Content = "Stop Recognition";
            }
            else
            {
                isRecognition = false;
                gestureRecognitionBtn.Content = "Start Recognition";
            }
        }

        
    }
}
