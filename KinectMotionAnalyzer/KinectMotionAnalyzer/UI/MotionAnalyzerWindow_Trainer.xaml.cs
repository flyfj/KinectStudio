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
using System.Data.Entity;
//using Emgu.CV;
//using Emgu.CV.Structure;
//using Emgu.Util;
using KinectMotionAnalyzer.Model;


namespace KinectMotionAnalyzer.UI
{
    using KinectMotionAnalyzer.Processors;


    /// <summary>
    /// Interaction logic for GestureRecognizerWindow.xaml
    /// </summary>
    public partial class MotionAnalyzerWindow_Trainer : Window
    {
        // tools
        private KinectDataManager kinect_data_manager;
        private KinectSensor kinect_sensor;
        private MotionAssessor motion_assessor = null;
        //private MotionDBContext dbcontext = null;  // database connection

        // recognition
        private GestureRecognizer gesture_recognizer = null;
        private FMSProcessor fmsProcessor = null;

        // sign
        bool isReplay = false;
        bool isRecognition = false;
        bool isStreaming = false;
        bool ifDoSmoothing = true;
        bool isCalculating = false; // a lock param for multi-threading

        // record params
        private string GESTURE_DATABASE_DIR = "gdata\\";
        private int frame_id = 0;
        private int MAX_ALLOW_FRAME = 800;  // no more than this number for color and skeleton to avoid memory issue
        Gesture temp_gesture = new Gesture();
        ArrayList overlap_frame_rec_buffer; // use to store record frames in memory
        List<Skeleton> skeleton_rec_buffer; // record skeleton data
        List<byte[]> color_frame_rec_buffer; // record video frames
        List<DepthImagePixel[]> depth_frame_rec_buffer;
        // we are not directly using action to store all buffer data since action has different
        // data type, e.g. skeleton which is harder to visualize

        // motion analysis params
        public List<MeasurementUnit> toMeasureUnits;


        public MotionAnalyzerWindow_Trainer()
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
                kinect_sensor.ColorFrameReady += kinect_colorframe_ready;
                kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;
                kinect_sensor.DepthFrameReady += kinect_depthframe_ready;
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

                if (gestureCaptureBtn.Content.ToString() == "Stop Capture")
                {
                    // consistent with skeleton data
                    //if (skeleton_rec_buffer.Count > 0)
                    //{
                        byte[] colorData = new byte[frame.PixelDataLength];
                        frame.CopyPixelDataTo(colorData);
                        
                        // remove oldest frame
                        if (color_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                            color_frame_rec_buffer.RemoveAt(0);

                        color_frame_rec_buffer.Add(colorData);

                        //Console.WriteLine(color_frame_rec_buffer.Count);
                    //}
                }

                kinect_data_manager.UpdateColorData(frame); 
            }
        }

        void kinect_depthframe_ready(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                if (gestureCaptureBtn.Content.ToString() == "Stop Capture")
                {
                    // consistent with skeleton data
                    if (skeleton_rec_buffer.Count > 0)
                    {
                        DepthImagePixel[] depthData = new DepthImagePixel[frame.PixelDataLength];
                        frame.CopyDepthImagePixelDataTo(depthData);

                        if (depth_frame_rec_buffer.Count == MAX_ALLOW_FRAME)
                            depth_frame_rec_buffer.RemoveAt(0);

                        depth_frame_rec_buffer.Add(depthData);
                    }
                }

                //kinect_data_manager.UpdateDepthData(frame);
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

                // if capturing, add to gesture data
                if (gestureCaptureBtn.Content.ToString() == "Stop Capture")
                {
                    if (skeleton_rec_buffer.Count == MAX_ALLOW_FRAME)
                        skeleton_rec_buffer.RemoveAt(0);

                    // just add first tracked skeleton, assume only one person is present
                    skeleton_rec_buffer.Add(tracked_skeleton);
                }

                if (tracked_skeleton == null)
                    return;

                if (kinect_data_manager.ifShowJointStatus)
                {
                    //if (!isCalculating)
                    //{
                    //    // start new thread to do processing
                    //    Thread thread = new Thread(motion_assessor.UpdateJointStatus);
                    //    thread.Start(
                    //}
                    // update status
                    motion_assessor.UpdateJointStatus(tracked_skeleton, toMeasureUnits);
                    kinect_data_manager.cur_joint_status = motion_assessor.GetCurrentJointStatus();
                    kinect_data_manager.toMeasureUnits = this.toMeasureUnits;

                    // show feedback
                    //feedback_textblock.Text = motion_assessor.GetFeedbackForCurrentStatus();
                }

                kinect_data_manager.UpdateSkeletonData(tracked_skeleton);
            }
        }

        #region gesture_management

        private void gestureCaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (gestureCaptureBtn.Content.ToString() == "Capture")
            {
                // check if type is selected
                if (actionComboBox.SelectedIndex <= 0)
                {
                    MessageBox.Show("Select an action type before capturing.");
                    return;
                }

                // reset
                frame_id = 0;
                color_frame_rec_buffer.Clear();
                skeleton_rec_buffer.Clear();
                gestureCaptureBtn.Content = "Stop Capture";
                
                // start kinect
                if (!kinect_sensor.IsRunning)
                {
                    // can't replay since share same gesture buffer
                    DeactivateReplay();
                    gestureReplayBtn.IsEnabled = false;

                    kinect_sensor.Start();
                }
            }
            else
            {
                if (kinect_sensor == null)
                    return;

                kinect_sensor.Stop();

                // prepare for replay
                if (skeleton_rec_buffer != null)
                {
                    ActivateReplay(color_frame_rec_buffer, skeleton_rec_buffer);
                    saveGestureBtn.IsEnabled = true;
                }

                gestureCaptureBtn.Content = "Capture";
                gestureReplayBtn.IsEnabled = true;
                isRecognition = false;

                processBtn.IsEnabled = true;
            }
        }

        private void saveGestureBtn_Click(object sender, RoutedEventArgs e)
        {
            //// save to file
            //string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            //string gesture_name = (gestureComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            //string savedir = "gdata\\"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            //if(!Directory.Exists(savedir))
            //    Directory.CreateDirectory(savedir);

            //string skeletonpath = savedir + gesture_name + "\\Kinect_skeleton_" + time + ".xml";

            // save to database
            statusbarLabel.Content = "Saving to database...";
            // save data from start label to end label
            int start_id = (int)skeletonVideoSlider.SelectionStart;
            int end_id = (int)skeletonVideoSlider.SelectionEnd;
            // remove end part first so front id will not change
            if (color_frame_rec_buffer.Count > 0 && 
                skeleton_rec_buffer.Count > 0 && 
                depth_frame_rec_buffer.Count > 0)
            {
                // clean data
                color_frame_rec_buffer.RemoveRange(end_id + 1, Math.Max(color_frame_rec_buffer.Count - end_id - 1, 0));
                color_frame_rec_buffer.RemoveRange(0, start_id);

                skeleton_rec_buffer.RemoveRange(end_id + 1, Math.Max(skeleton_rec_buffer.Count - end_id - 1, 0));
                skeleton_rec_buffer.RemoveRange(0, start_id);

                //depth_frame_rec_buffer.RemoveRange(end_id + 1, Math.Max(depth_frame_rec_buffer.Count - end_id - 1, 0));
                //depth_frame_rec_buffer.RemoveRange(0, start_id);

                // convert to kinect action for saving
                KinectAction rec_action = new KinectAction();
                rec_action.ActionName = (actionComboBox.SelectedItem as ComboBoxItem).Content.ToString();
                rec_action.ColorFrames = new List<ColorFrameData>();
                rec_action.Skeletons = new List<SkeletonData>();
                rec_action.DepthFrames = new List<DepthMapData>();

                // copy color frame
                for (int i = 0; i < color_frame_rec_buffer.Count; i++)
                {
                    ColorFrameData colorFrame = new ColorFrameData();
                    colorFrame.FrameData = color_frame_rec_buffer[i];
                    colorFrame.FrameWidth = kinect_sensor.ColorStream.FrameWidth;
                    colorFrame.FrameHeight = kinect_sensor.ColorStream.FrameHeight;
                    colorFrame.FrameId = i;

                    rec_action.ColorFrames.Add(colorFrame);
                }
                // copy depth frame
                for (int i = 0; i < depth_frame_rec_buffer.Count; i++)
                {
                    DepthMapData depthFrame = new DepthMapData();
                    depthFrame.FrameWidth = kinect_sensor.DepthStream.FrameWidth;
                    depthFrame.FrameHeight = kinect_sensor.DepthStream.FrameHeight;
                    depthFrame.FrameId = i;
                    //depthFrame.DepthData = new short[depth_frame_rec_buffer[i].Length];
                    // copy depth
                    //for (int j = 0; j < depth_frame_rec_buffer[i].Length; j++)
                    //    depthFrame.DepthData[j] = depth_frame_rec_buffer[i][j].Depth;

                    rec_action.DepthFrames.Add(depthFrame);
                }
                // copy skeleton
                for (int i = 0; i < skeleton_rec_buffer.Count; i++)
                {
                    SkeletonData skeData = new SkeletonData();
                    if (skeleton_rec_buffer[i] != null)
                    {
                        skeData.Status = (int)skeleton_rec_buffer[i].TrackingState;
                        skeData.JointsData = new List<SingleJoint>();
                        foreach (JointType jtype in Enum.GetValues(typeof(JointType)))
                        {
                            SingleJoint cur_joint = new SingleJoint();
                            cur_joint.PosX = skeleton_rec_buffer[i].Joints[jtype].Position.X;
                            cur_joint.PosY = skeleton_rec_buffer[i].Joints[jtype].Position.Y;
                            cur_joint.PosZ = skeleton_rec_buffer[i].Joints[jtype].Position.Z;
                            cur_joint.Type = (int)jtype;
                            cur_joint.TrackingStatus = (int)skeleton_rec_buffer[i].Joints[jtype].TrackingState;
                            skeData.JointsData.Add(cur_joint);
                        }
                    }

                    rec_action.Skeletons.Add(skeData);
                }

                if (KinectRecorder.WriteActionToDatabase(rec_action))
                    statusbarLabel.Content = "Finish saving to database: " + 
                        color_frame_rec_buffer.Count + "frames";
                else
                    statusbarLabel.Content = "Fail to save to database.";
            }
            
            //KinectRecorder.WriteToSkeletonXMLFile(skeletonpath, skeleton_rec_buffer);
            //skeleton_rec_buffer.Clear();
            //frame_id = 0; 
            //statusbarLabel.Content = "Save skeletons to file: " + skeletonpath;

            color_frame_rec_buffer.Clear();
            skeleton_rec_buffer.Clear();

            statusbarLabel.Content = "Save action to database.";

            DeactivateReplay();
            saveGestureBtn.IsEnabled = false;
        }

        private void add_gesture_btn_Click(object sender, RoutedEventArgs e)
        {
            // open add window
            GestureConfigWin add_win = new GestureConfigWin();
            if (add_win.ShowDialog().Value == true)
            {
                // add action type to database
                // check duplicate
                if (actionComboBox.Items.Contains(add_win.new_gesture_config.name))
                {
                    MessageBox.Show("This action name exists already. Change another name.");
                    return;
                }

                using (MotionDBContext motionContext = new MotionDBContext())
                {
                    try
                    {
                        ActionType new_type = new ActionType();
                        new_type.Name = add_win.new_gesture_config.name;
                        motionContext.ActionTypes.Add(new_type);
                        motionContext.SaveChanges();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

                //gesture_recognizer.AddGestureConfig(add_win.new_gesture_config);

                // update list
                UpdateGestureComboBox();
                // update status bar
                statusbarLabel.Content = "Add new action: " + add_win.new_gesture_config.name;
            }
        }

        private void remove_gesture_btn_Click(object sender, RoutedEventArgs e)
        {
            // remove gesture config of current selected one
            int gid = actionComboBox.SelectedIndex;
            // can't delete default one
            if (gid == 0)
            {
                MessageBox.Show("Select a valid gesture to remove.");
                return;
            }

            ComboBoxItem toRemoveItem = actionComboBox.Items[gid] as ComboBoxItem;
            try
            {
                // remove type
                using (MotionDBContext dbcontext = new MotionDBContext())
                {
                    string toRemoveString = toRemoveItem.Content.ToString();
                    ActionType toRemoveType = dbcontext.ActionTypes.FirstOrDefault(x => x.Name == toRemoveString);
                    dbcontext.ActionTypes.Remove(toRemoveType);
                    // TODO: remove actions
                    //var res = dbcontext.Actions.Select(x => x.ActionName == toRemoveItem.Content.ToString());

                    // update
                    dbcontext.SaveChanges();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //gesture_recognizer.RemoveGestureConfig(toRemoveItem.Content.ToString());

            // update ui
            UpdateGestureComboBox();
            // update status bar
            statusbarLabel.Content = "Remove action: " + toRemoveItem.Content;
        }

        #endregion

        private void UpdateGestureComboBox()
        {

            actionComboBox.Items.Clear();
            // add prompt item
            ComboBoxItem prompt = new ComboBoxItem();
            prompt.Content = "Choose Gesture";
            prompt.IsEnabled = false;
            prompt.IsSelected = true;
            actionComboBox.Items.Add(prompt);

            // update from database
            try
            {
                using (MotionDBContext dbcontext = new MotionDBContext())
                {
                    foreach (ActionType action_type in dbcontext.ActionTypes)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = action_type.Name;
                        actionComboBox.Items.Add(item);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //// add item for each gesture type
            //foreach (string gname in gesture_recognizer.GESTURE_LIST.Values)
            //{
            //    ComboBoxItem item = new ComboBoxItem();
            //    item.Content = gname;
            //    gestureComboBox.Items.Add(item);
            //}
        }

        #region gesture_replay

        private void skeletonVideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // valid only when kinect is stopped so no new data will come
            if (isReplay && color_frame_rec_buffer.Count > 0 && skeleton_rec_buffer.Count > 0)
            {
                // load new skeleton data
                int cur_frame_id = (int)skeletonVideoSlider.Value;
                if (skeleton_rec_buffer.Count > cur_frame_id && color_frame_rec_buffer.Count > cur_frame_id)
                {
                    kinect_data_manager.UpdateColorData(color_frame_rec_buffer[cur_frame_id], 640, 480);
                    kinect_data_manager.UpdateSkeletonData(skeleton_rec_buffer[cur_frame_id]);

                    // update label
                    skeletonSliderLabel.Content = skeletonVideoSlider.Value.ToString();
                }
            }
        }

        private void gestureReplayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor != null && kinect_sensor.IsRunning)
                return;

            // read action bank from database and populate window control to show
            ActionDatabasePreview preview_win = new ActionDatabasePreview();
            preview_win.dbActionTypeList.Items.Clear();
            try
            {
                using (MotionDBContext dbcontext = new MotionDBContext())
                {
                    foreach (ActionType cur_type in dbcontext.ActionTypes)
                    {
                        preview_win.dbActionTypeList.Items.Add(cur_type.Name);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (preview_win.ShowDialog().Value)
            {
                // get replay action id
                if (preview_win.selectedActionId < 0)
                    return;

                // retrieve action from database
                try
                {
                    using (MotionDBContext dbcontext = new MotionDBContext())
                    {
                        MessageBox.Show(dbcontext.Database.Connection.ConnectionString);

                        //foreach (KinectAction ac in dbcontext.Actions)
                        //{
                        //    if (ac.ColorFrames != null)
                        //        MessageBox.Show(ac.ColorFrames.Count.ToString());
                            
                        //}
                        //return;

                        var query = from ac in dbcontext.Actions.Include("ColorFrames").Include("DepthFrames").Include("Skeletons.JointsData")
                                    where ac.Id == preview_win.selectedActionId
                                    select ac;

                        if (query != null)
                        {
                            foreach (var q in query)
                            {
                                KinectAction sel_action = q as KinectAction;
                                if (!Tools.ConvertFromKinectAction(
                                        sel_action,
                                        out color_frame_rec_buffer,
                                        out depth_frame_rec_buffer,
                                        out skeleton_rec_buffer))
                                {
                                    MessageBox.Show("Fail to load database action.");
                                    return;
                                }
                                break;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                                          
                // activate replay
                ActivateReplay(color_frame_rec_buffer, skeleton_rec_buffer);
            }

            // obsolete format
            //OpenFileDialog dialog = new OpenFileDialog();
            //dialog.DefaultExt = ".xml";
            //dialog.FileName = "Skeleton";
            //dialog.Filter = "Skeleton data file (.xml)|*.xml";

            //Nullable<bool> result = dialog.ShowDialog();

            //if (result == true)
            //{
            //    string filename = dialog.FileName;
            //    // test: read skeleton data and display
            //    KinectRecorder.ReadFromSkeletonXMLFile(filename, out skeleton_rec_buffer);
            //    statusbarLabel.Content = "Load gesture file from " + filename;
            //    ActivateReplay(color_frame_rec_buffer, skeleton_rec_buffer);
            //    isReplay = true;
            //}
        }

        private void ActivateReplay(List<byte[]> color_frame_rec_buffer, List<Skeleton> skeleton_rec_buffer)
        {
            if (color_frame_rec_buffer == null || color_frame_rec_buffer.Count == 0
                 || skeleton_rec_buffer == null || skeleton_rec_buffer.Count == 0)
            {
                statusbarLabel.Content = "Replay gesture is empty.";
                return;
            }

            int min_frame_id = 0;
            int max_frame_id = Math.Min(color_frame_rec_buffer.Count, skeleton_rec_buffer.Count) - 1;

            skeletonVideoSlider.IsEnabled = true;
            skeletonVideoSlider.Minimum = min_frame_id;
            skeletonVideoSlider.Maximum = max_frame_id;
            skeletonVideoSlider.SelectionStart = min_frame_id;
            skeletonVideoSlider.SelectionEnd = max_frame_id;
            skeletonVideoSlider.Value = min_frame_id;
            skeletonSliderLabel.Content = min_frame_id.ToString();

            replay_setStartBtn.IsEnabled = true;
            replay_startLabel.Content = min_frame_id.ToString();
            //replay_setEndBtn.IsEnabled = true;
            replay_endLabel.Content = max_frame_id.ToString();

            keyframeConfigBtn.IsEnabled = true;

            isReplay = true;

            // update view
            kinect_data_manager.UpdateColorData(color_frame_rec_buffer[min_frame_id], 640, 480);
            kinect_data_manager.UpdateSkeletonData(skeleton_rec_buffer[min_frame_id]);
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

            keyframeConfigBtn.IsEnabled = false;

            isReplay = false;
        }

        private void replay_setStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!skeletonVideoSlider.IsSelectionRangeEnabled)
                skeletonVideoSlider.IsSelectionRangeEnabled = true;

            skeletonVideoSlider.SelectionStart = skeletonVideoSlider.Value;
            replay_startLabel.Content = skeletonVideoSlider.Value.ToString();

            replay_setEndBtn.IsEnabled = true;
        }

        private void replay_setEndBtn_Click(object sender, RoutedEventArgs e)
        {
            if (skeletonVideoSlider.Value < skeletonVideoSlider.SelectionStart)
            {
                MessageBox.Show("End frame can't be earlier than start frame.");
                return;
            }

            skeletonVideoSlider.SelectionEnd = skeletonVideoSlider.Value;
            replay_endLabel.Content = skeletonVideoSlider.Value;
        }

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect_sensor != null)
                kinect_sensor.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // do initialization here
            gesture_recognizer = new GestureRecognizer();
            motion_assessor = new MotionAssessor();
            fmsProcessor = new FMSProcessor();
            toMeasureUnits = new List<MeasurementUnit>();
            overlap_frame_rec_buffer = new ArrayList();
            skeleton_rec_buffer = new List<Skeleton>();
            color_frame_rec_buffer = new List<byte[]>();
            depth_frame_rec_buffer = new List<DepthImagePixel[]>();

            // init kinect
            if (!InitKinect())
            {
                statusbarLabel.Content = "Kinect not connected";
                MessageBox.Show("Kinect not found.");
            }
            else
                statusbarLabel.Content = "Kinect initialized";

            DeactivateReplay();

            // load gesture config and update ui
            //gesture_recognizer.LoadAllGestureConfig();
            UpdateGestureComboBox();
        }

        private void measureConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            MeasurementConfigWin measureConfigWin = new MeasurementConfigWin();
            //measureConfigWin.measureUnits = this.toMeasureUnits;    // restore previously selected ones
            
            if (measureConfigWin.ShowDialog().Value == true)
                toMeasureUnits = measureConfigWin.measureUnits;
            
        }

        private void processBtn_Click(object sender, RoutedEventArgs e)
        {
            int sel_test_id = Math.Max(actionComboBox.SelectedIndex - 1, 0);
            // trim valid buffer data
            statusbarLabel.Content = "Processing " + actionComboBox.Items[sel_test_id+1].ToString();
            // save data from start label to end label
            int start_id = (int)skeletonVideoSlider.SelectionStart;
            int end_id = (int)skeletonVideoSlider.SelectionEnd;
            // remove end part first so front id will not change
            if (skeleton_rec_buffer.Count > 0)
            {
                // clean data
                //color_frame_rec_buffer.RemoveRange(end_id + 1, Math.Max(color_frame_rec_buffer.Count - end_id - 1, 0));
                //color_frame_rec_buffer.RemoveRange(0, start_id);

                skeleton_rec_buffer.RemoveRange(end_id + 1, Math.Max(skeleton_rec_buffer.Count - end_id - 1, 0));
                skeleton_rec_buffer.RemoveRange(0, start_id);
            }

            FMSTestEvaluation test_eval = fmsProcessor.EvaluateTest(skeleton_rec_buffer, sel_test_id);

            FMSReportWindow reportWin = new FMSReportWindow();
            reportWin.groupBox.Header = fmsProcessor.FMSTests[sel_test_id].testName;
            reportWin.ScoreLabel1.Content = "Score: " + test_eval.testScore;
            reportWin.ruleBox1.Content = fmsProcessor.FMSTests[sel_test_id].rules[0].name;
            reportWin.ruleBox1.IsChecked = (test_eval.rule_evals[0].ruleScore == 1 ? true : false);
            reportWin.ruleBox2.Content = fmsProcessor.FMSTests[sel_test_id].rules[1].name;
            reportWin.ruleBox2.IsChecked = (test_eval.rule_evals[1].ruleScore == 1 ? true : false);
            reportWin.ruleBox3.Content = fmsProcessor.FMSTests[sel_test_id].rules[2].name;
            reportWin.ruleBox3.IsChecked = (test_eval.rule_evals[2].ruleScore == 1 ? true : false);
            //reportWin.ruleBox4.Content = fmsProcessor.FMSTests[sel_test_id].rules[3].name;
            //reportWin.ruleBox4.IsChecked = (test_eval.rule_evals[3].ruleScore == 1 ? true : false);
            reportWin.Show();

            //return;
            
        }

    }
}
