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
using System.Diagnostics;


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
        private MotionAssessor motion_assessor = new MotionAssessor();

        // recognition
        private GestureRecognizer gesture_recognizer = new GestureRecognizer();
        private string GESTURE_DATABASE_DIR = "D:\\gdata\\";

        // sign
        bool isReplay = false;
        bool isRecognition = false;
        bool isStreaming = false;
        
        // record params
        private int frame_id = 0;
        List<Skeleton> gesture_capture_data = new List<Skeleton>();
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

            // load gesture config and update ui
            gesture_recognizer.LoadAllGestureConfig();
            UpdateGestureComboBox();

            //temp_gesture.data = new List<Skeleton>();

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
                //replay_data_manager = new KinectDataManager(ref kinect_sensor);

                // initialize stream
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                kinect_sensor.SkeletonStream.Enable();

                // set source (must after source has been initialized otherwise it's null forever)
                kinect_data_manager.ColorStreamBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);
                color_disp_img.Source = kinect_data_manager.ColorStreamBitmap;
                ske_disp_img.Source = kinect_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.ColorFrameReady += kinect_colorframe_ready;
                kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;
            }
            else
            {
                // invalidate all buttons
                gestureReplayBtn.IsEnabled = false;

                return false;
            }

            return true;
        }


        void kinect_colorframe_ready(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateColorData(frame);
            }
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

                if (tracked_skeleton == null)
                    return;

                // if capturing, add to gesture data
                if (gestureCaptureBtn.Content.ToString() == "Stop Capture")
                {
                    // just add first tracked skeleton, assume only one person is present
                    gesture_capture_data.Add(tracked_skeleton);
                }

                if (isRecognition)
                {
                    #region update_gesture_data

                    if (temp_gesture.data.Count >= gesture_recognizer.gesture_max_len)
                    {
                        temp_gesture.data.RemoveAt(0);
                        Debug.WriteLine("Remove frame.");
                    }

                    temp_gesture.data.Add(tracked_skeleton);
                    Debug.WriteLine("Add frame:" + temp_gesture.data.Count.ToString());

                    #endregion
                    

                    if(temp_gesture.data.Count >= gesture_recognizer.gesture_min_len/2 && 
                        temp_gesture.data.Count <= gesture_recognizer.gesture_max_len*2)
                    {
                        // reset
                        gesture_match_scorebar.Value = gesture_match_scorebar.Maximum;
                        recDistLabel.Content = gesture_match_scorebar.Maximum;
                        rec_res_label.Content = "Unknown";
                        Debug.WriteLine("reset");

                        Debug.WriteLine("Do recognition.");

                        // do recognition
                        string res = "";
                        double dist = gesture_recognizer.MatchToDatabase(temp_gesture, out res);
                        gesture_match_scorebar.Value = 
                            (double.IsInfinity(dist) ? gesture_match_scorebar.Maximum : dist);
                        if (dist >=0 && dist <= 20)
                        {
                            rec_res_label.Content = res;
                            last_detection_label.Content = res;
                            temp_gesture.data.Clear();
                            Debug.WriteLine("Detected");
                        }
                        else
                            rec_res_label.Content = "Unknown";

                        recDistLabel.Content = dist.ToString();
                    }
                }

                if(kinect_data_manager.ifShowJointStatus)
                {
                    // update status
                    motion_assessor.UpdateJointStatus(tracked_skeleton);
                    kinect_data_manager.cur_joint_status = motion_assessor.GetCurrentJointStatus();

                    // show feedback
                    feedback_textblock.Text = motion_assessor.GetFeedbackForCurrentStatus();
                }

                kinect_data_manager.UpdateSkeletonData(skeletons);
            }
        }

#region gesture_management

        private void gestureCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (gestureCaptureBtn.Content.ToString() == "Capture")
            {
                // check if type is selected
                if (gestureComboBox.SelectedIndex <= 0)
                {
                    MessageBox.Show("Select a gesture type before capturing.");
                    return;
                }

                // reset
                frame_id = 0;
                gesture_capture_data.Clear();
                gestureCaptureBtn.Content = "Stop Capture";
                gestureRecognitionBtn.IsEnabled = false;
                previewBtn.IsEnabled = false;

                // start kinect
                if (kinect_sensor == null)
                    return;

                if (!kinect_sensor.IsRunning)
                {
                    // can't replay since share same gesture buffer
                    DeactivateReplay();
                    gestureReplayBtn.IsEnabled = false;
                    gestureRecognitionBtn.IsEnabled = false;

                    kinect_sensor.Start();
                }

            }
            else
            {
                kinect_sensor.Stop();

                // prepare for replay
                if (gesture_capture_data != null)
                {
                    ActivateReplay(gesture_capture_data);
                    saveGestureBtn.IsEnabled = true;
                }

                gestureCaptureBtn.Content = "Capture";
                gestureReplayBtn.IsEnabled = true;
                gestureRecognitionBtn.IsEnabled = true;
                previewBtn.IsEnabled = true;
                isRecognition = false;
            }
        }

        private void saveGestureBtn_Click(object sender, RoutedEventArgs e)
        {
            // save to file
            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string gesture_name = (gestureComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            string myPhotos = "D:"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string skeletonpath = myPhotos + "\\gdata\\" + gesture_name + "\\Kinect_skeleton_" + time + ".xml";

            // save data from start label to end label
            int start_id = int.Parse(replay_startLabel.Content.ToString());
            int end_id = int.Parse(replay_endLabel.Content.ToString());
            // remove end part first so front id will not change
            gesture_capture_data.RemoveRange(end_id + 1, gesture_capture_data.Count - end_id - 1);
            gesture_capture_data.RemoveRange(0, start_id);
            KinectRecorder.WriteToSkeletonFile(skeletonpath, gesture_capture_data);

            gesture_capture_data.Clear();
            frame_id = 0;

            statusbarLabel.Content = "Save skeletons to file: " + skeletonpath;

            DeactivateReplay();
            saveGestureBtn.IsEnabled = false;
        }

        private void add_gesture_btn_Click(object sender, RoutedEventArgs e)
        {
            // open add window
            GestureConfigWin add_win = new GestureConfigWin();
            if (add_win.ShowDialog().Value == true)
            {
                gesture_recognizer.AddGestureConfig(add_win.new_gesture_config);

                // update list
                UpdateGestureComboBox();
                // update status bar
                statusbarLabel.Content = "Add gesture: " + add_win.new_gesture_config.name;
            }
        }

        private void remove_gesture_btn_Click(object sender, RoutedEventArgs e)
        {
            // remove gesture config of current selected one
            int gid = gestureComboBox.SelectedIndex;
            if (gid == 0)    // can't delete default one
            {
                MessageBox.Show("Select a valid gesture to remove.");
                return;
            }

            ComboBoxItem toRemoveItem = gestureComboBox.Items[gid] as ComboBoxItem;
            gesture_recognizer.RemoveGestureConfig(toRemoveItem.Content.ToString());

            // update ui
            UpdateGestureComboBox();
            // update status bar
            statusbarLabel.Content = "Remove gesture: " + toRemoveItem.Content;
        }

#endregion

        private void UpdateGestureComboBox()
        {

            gestureComboBox.Items.Clear();
            // add prompt item
            ComboBoxItem prompt = new ComboBoxItem();
            prompt.Content = "Choose Gesture";
            prompt.IsEnabled = false;
            prompt.IsSelected = true;
            gestureComboBox.Items.Add(prompt);

            // add item for each gesture type
            foreach (string gname in gesture_recognizer.GESTURE_LIST.Values)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = gname;
                gestureComboBox.Items.Add(item);
            }

        }

#region gesture_replay

        private void skeletonVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (isReplay && gesture_capture_data.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)skeletonVideoSlider.Value;
                if (gesture_capture_data.Count > cur_frame_id)
                {
                    kinect_data_manager.UpdateSkeletonData(gesture_capture_data[cur_frame_id]);
                    //replay_data_manager.UpdateSkeletonData(gesture_capture_data[cur_frame_id]);
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
                gesture_capture_data = KinectRecorder.ReadFromSkeletonFile(filename);

                statusbarLabel.Content = "Load gesture file from " + filename;

                ActivateReplay(gesture_capture_data);

                isReplay = true;
            }

        }

        private void ActivateReplay(List<Skeleton> gesture)
        {
            if (gesture == null || gesture.Count == 0)
            {
                statusbarLabel.Content = "Replay gesture is empty.";
                return;
            }

            //gesture_data = gesture;

            int min_frame_id = 0;
            int max_frame_id = gesture.Count - 1;

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

            kinect_data_manager.UpdateSkeletonData(gesture[min_frame_id]);
            //replay_data_manager.UpdateSkeletonData(gesture[min_frame_id]);
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
            if (skeletonVideoSlider.Value < double.Parse(replay_startLabel.Content.ToString()))
            {
                MessageBox.Show("End frame can't be earlier than start frame.");
                return;
            }

            replay_endLabel.Content = skeletonVideoSlider.Value;
        }

#endregion
        
        private void gestureRecognitionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecognition)
            {
                if (gesture_recognizer == null)
                {
                    gesture_recognizer = new GestureRecognizer();
                }

                if (!gesture_recognizer.LoadGestureDatabase(GESTURE_DATABASE_DIR))
                {
                    MessageBox.Show("No gesture database found for recognition.");
                    return;
                }

                UpdateGestureComboBox();

                // can't replay since share same gesture buffer
                DeactivateReplay();
                gestureReplayBtn.IsEnabled = false;

                // start kinect
                if (kinect_sensor == null)
                    return;

                if (!kinect_sensor.IsRunning)
                    kinect_sensor.Start();

                isRecognition = true;
                gestureCaptureBtn.IsEnabled = false;
                previewBtn.IsEnabled = false;

                // can't edit gesture
                add_gesture_btn.IsEnabled = false;
                remove_gesture_btn.IsEnabled = false;
                gestureRecognitionBtn.Content = "Stop Recognition";
            }
            else
            {
                kinect_sensor.Stop();

                isRecognition = false;
                recDistLabel.Content = 0;
                gestureCaptureBtn.IsEnabled = true;
                previewBtn.IsEnabled = true;

                //enable gesture editing
                add_gesture_btn.IsEnabled = true;
                remove_gesture_btn.IsEnabled = true;
                gestureRecognitionBtn.Content = "Start Recognition";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect_sensor != null)
                kinect_sensor.Stop();
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (previewBtn.Content.ToString() == "Preview Stream")
            {
                if (kinect_sensor != null)
                {
                    // disable all other buttons
                    DeactivateReplay();
                    gestureCaptureBtn.IsEnabled = false;
                    gestureRecognitionBtn.IsEnabled = false;
                    gestureReplayBtn.IsEnabled = false;
                    previewBtn.Content = "Stop Stream";

                    kinect_sensor.Start();
                    isStreaming = true;
                    kinect_data_manager.ifShowJointStatus = true;
                }
            }
            else
            {
                if(kinect_sensor != null)
                {
                    gestureCaptureBtn.IsEnabled = true;
                    gestureReplayBtn.IsEnabled = true;
                    gestureRecognitionBtn.IsEnabled = true;
                    previewBtn.Content = "Preview Stream";

                    kinect_sensor.Stop();

                    isStreaming = false;
                    kinect_data_manager.ifShowJointStatus = false;
                }
            }
            
            
        } 

    }
}
