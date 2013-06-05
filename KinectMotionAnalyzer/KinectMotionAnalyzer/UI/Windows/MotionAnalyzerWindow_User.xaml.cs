using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using Microsoft.Kinect;
using Microsoft.Win32;
using System.Diagnostics;
//using Emgu.CV;
//using Emgu.CV.Structure;
//using Emgu.Util;
using KinectMotionAnalyzer.DataModel;


namespace KinectMotionAnalyzer.UI
{
    using KinectMotionAnalyzer.Processors;


    /// <summary>
    /// Interaction logic for GestureRecognizerWindow.xaml
    /// </summary>
    public partial class MotionAnalyzerWindow_User : Window
    {
        // tools
        private KinectDataManager kinect_data_manager;
        private KinectSensor kinect_sensor;
        private MotionAssessor motion_assessor = null;

        //private GestureRecognizer gesture_recognizer = null;

        // sign
        bool isStreaming = false;
        bool ifDoSmoothing = true;

        // record params
        ArrayList overlap_frame_rec_buffer; // use to store record frames in memory
        List<Skeleton> skeleton_rec_buffer; // record skeleton data
        List<byte[]> color_frame_rec_buffer; // record video frames

        // motion analysis params
        private Dictionary<string, List<MeasurementUnit>> actionMeasureUnitsCollection = null;
        private List<MeasurementUnit> toMeasureUnits = null;


        public MotionAnalyzerWindow_User()
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
                kinect_data_manager = new KinectDataManager(ref kinect_sensor);
                //replay_data_manager = new KinectDataManager(ref kinect_sensor);

                // initialize stream
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
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

                        // Smoothed with some latency.
                        // Filters out medium jitters.
                        // Good for a menu system that needs to be smooth but
                        // doesn't need the reduced latency as much as gesture recognition does.
                        //smoothingParam.Smoothing = 0.5f;
                        //smoothingParam.Correction = 0.1f;
                        //smoothingParam.Prediction = 0.5f;
                        //smoothingParam.JitterRadius = 0.1f;
                        //smoothingParam.MaxDeviationRadius = 0.1f;

                        //// Very smooth, but with a lot of latency.
                        //// Filters out large jitters.
                        //// Good for situations where smooth data is absolutely required
                        //// and latency is not an issue.
                        //smoothingParam.Smoothing = 0.7f;
                        //smoothingParam.Correction = 0.3f;
                        //smoothingParam.Prediction = 1.0f;
                        //smoothingParam.JitterRadius = 1.0f;
                        //smoothingParam.MaxDeviationRadius = 1.0f;
                    };

                    kinect_sensor.SkeletonStream.Enable(smoothingParam);
                }
                else
                    kinect_sensor.SkeletonStream.Enable();


                // set source (must after source has been initialized otherwise it's null forever)
                kinect_data_manager.ColorStreamBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);
                color_disp_img.Source = kinect_data_manager.ColorStreamBitmap;
                ske_disp_img.Source = kinect_data_manager.skeletonImageSource;

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
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateColorData(frame);
            }

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

                if (kinect_data_manager.ifShowJointStatus)
                {
                    // update status
                    motion_assessor.UpdateJointStatus(tracked_skeleton, toMeasureUnits);
                    kinect_data_manager.cur_joint_status = motion_assessor.GetCurrentJointStatus();
                    //kinect_data_manager.toMeasureUnits = this.toMeasureUnits;

                    // show feedback
                    //feedback_textblock.Text = motion_assessor.GetFeedbackForCurrentStatus();
                }

                kinect_data_manager.UpdateSkeletonData(tracked_skeleton);
            }

            
        }

        #region action_management

        private void add_action_btn_Click(object sender, RoutedEventArgs e)
        {
            // open add window
            MeasurementConfigWin add_win = new MeasurementConfigWin();
            if (add_win.ShowDialog().Value == true)
            {
                if (actionMeasureUnitsCollection.ContainsKey(add_win.newActionName))
                    MessageBox.Show("Action name already exists.");
                else
                {
                    actionMeasureUnitsCollection.Add(add_win.newActionName, add_win.measureUnits);
                    // update list
                    UpdateActionComboBox();
                    // update status bar
                    statusbarLabel.Content = "Add action: " + add_win.newActionName;
                }
            }
        }

        private void remove_action_btn_Click(object sender, RoutedEventArgs e)
        {
            // remove gesture config of current selected one
            if (actionComboBox.SelectedIndex <= 0)    // can't delete default one
            {
                MessageBox.Show("Select a valid action to remove.");
                return;
            }

            string toRemoveActionName = actionComboBox.SelectionBoxItem.ToString();
            actionMeasureUnitsCollection.Remove(toRemoveActionName);

            // update ui
            UpdateActionComboBox();
            // update status bar
            statusbarLabel.Content = "Remove action: " + toRemoveActionName;
        }

        #endregion

        private void UpdateActionComboBox()
        {
            actionComboBox.Items.Clear();
            // add prompt item
            ComboBoxItem prompt = new ComboBoxItem();
            prompt.Content = "Choose Gesture";
            prompt.IsEnabled = false;
            prompt.IsSelected = true;
            actionComboBox.Items.Add(prompt);

            // add item for each gesture type
            foreach (string actionName in actionMeasureUnitsCollection.Keys)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = actionName;
                actionComboBox.Items.Add(item);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect_sensor != null)
                kinect_sensor.Stop();
        }

        private void previewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isStreaming)
            {
                if (actionComboBox.SelectedIndex <= 0)
                {
                    MessageBox.Show("Select a valid action");
                    return;
                }

                color_frame_rec_buffer.Clear();
                skeleton_rec_buffer.Clear();

                if (kinect_sensor != null)
                {
                    // disable all other buttons
                    previewBtn.Content = "Stop Test";
                    isStreaming = true;
                    kinect_data_manager.ifShowJointStatus = true;

                    toMeasureUnits = actionMeasureUnitsCollection[actionComboBox.SelectionBoxItem.ToString()];
                    kinect_data_manager.toMeasureUnits = this.toMeasureUnits;

                    overlap_frame_rec_buffer.Clear();

                    kinect_sensor.Start();
                }
            }
            else
            {
                if (kinect_sensor != null)
                {
                    kinect_sensor.Stop();

                    isStreaming = false;
                    kinect_data_manager.ifShowJointStatus = false;

                    // save recorded frame to disk and save skeleton data
                    //if (overlap_frame_rec_buffer!=null && skeleton_rec_buffer!=null && saveVideoCheckBox.IsChecked.Value)
                    //{
                    //    statusbarLabel.Content = "Saving video...";

                    //    // create video writer
                    //    int fwidth = (int)groupBox3.Width + 20;
                    //    int fheight = (int)groupBox3.Height + 20;

                    //    SaveFileDialog saveDialog = new SaveFileDialog();
                    //    saveDialog.Filter = "avi files (*.avi)|*.avi";
                    //    saveDialog.FilterIndex = 2;
                    //    saveDialog.RestoreDirectory = true;

                    //    //if (saveDialog.ShowDialog().Value)
                    //    //{
                    //    //    string videofile = saveDialog.FileName.ToString();
                    //    //    VideoWriter videoWriter = new VideoWriter(videofile, CvInvoke.CV_FOURCC('M', 'J', 'P', 'G'), 15,
                    //    //        fwidth, fheight, true);

                    //    //    // save video
                    //    //    if (videoWriter == null)
                    //    //        MessageBox.Show("Fail to save video. Check if codec has been installed.");
                    //    //    else
                    //    //    {
                    //    //        for (int i = 0; i < overlap_frame_rec_buffer.Count; i++)
                    //    //        {
                    //    //            // write to video file
                    //    //            Emgu.CV.Image<Bgr, byte> cvImg =
                    //    //                new Emgu.CV.Image<Bgr, byte>(overlap_frame_rec_buffer[i] as Bitmap);

                    //    //            videoWriter.WriteFrame<Bgr, byte>(cvImg);
                    //    //        }

                    //    //        videoWriter.Dispose();

                    //    //        statusbarLabel.Content = "Video saved to " + videofile;
                    //    //    }

                    //    //    // save skeleton
                    //    //    string skeletonpath = videofile + ".xml";
                    //    //    KinectRecorder.WriteToSkeletonXMLFile(skeletonpath, skeleton_rec_buffer);
                    //    //}
                    //}

                    skeleton_rec_buffer.Clear();
                    overlap_frame_rec_buffer.Clear();

                    previewBtn.Content = "Start Test";

                    // save tracked elbow speed
                    //FileStream file = File.Open("d:\\temp\\test.txt", FileMode.Create);
                    //StreamWriter writer = new StreamWriter(file);
                    //for (int i = 0; i < motion_assessor.jointStatusSeq.Count; i++)
                    //    writer.WriteLine(motion_assessor.jointStatusSeq[i][JointType.HandRight].abs_speed);
                    //writer.Close();
                }
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // do initialization here
            //gesture_recognizer = new GestureRecognizer();
            motion_assessor = new MotionAssessor();
            toMeasureUnits = new List<MeasurementUnit>();
            overlap_frame_rec_buffer = new ArrayList();
            skeleton_rec_buffer = new List<Skeleton>();
            color_frame_rec_buffer = new List<byte[]>();
            actionMeasureUnitsCollection = new Dictionary<string, List<MeasurementUnit>>();

            // init kinect
            if (!InitKinect())
            {
                statusbarLabel.Content = "Kinect not connected";
                MessageBox.Show("Kinect not found.");
            }
            else
                statusbarLabel.Content = "Kinect initialized";

            // load gesture config and update ui
            //gesture_recognizer.LoadAllGestureConfig();
            UpdateActionComboBox();
        }

        private void measureConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            MeasurementConfigWin measureConfigWin = new MeasurementConfigWin();
            //measureConfigWin.measureUnits = this.toMeasureUnits;    // restore previously selected ones
            
            if (measureConfigWin.ShowDialog().Value == true)
                toMeasureUnits = measureConfigWin.measureUnits;
            
        }

    }
}
