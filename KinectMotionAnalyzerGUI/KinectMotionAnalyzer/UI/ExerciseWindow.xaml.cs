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
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Win32;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using KinectMotionAnalyzer.Processors;
using System.Windows.Media.Animation;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;


namespace KinectMotionAnalyzer.UI
{

    /// <summary>
    /// Interaction logic for GestureRecognizerWindow.xaml
    /// </summary>
    public partial class ExerciseWindow : Window
    {
        // tools
        private KinectDataManager kinect_data_manager;
        private KinectSensor kinect_sensor;
        private MotionAssessor motion_assessor = new MotionAssessor();
        public List<MeasurementUnit> toMeasureUnits {get; set;} 
        private readonly KinectSensorChooser sensorChooser;

        // recognition
        private GestureRecognizer gesture_recognizer = new GestureRecognizer();
        private string GESTURE_DATABASE_DIR = "C:\\gdata\\";

        // sign
        bool isReplay = false;
        bool isRecognition = false;
        bool isStreaming = false;
        bool ifDoSmoothing = true;
        bool isRecording = false;

        // record params
        private int frame_id = 0;
        List<Skeleton> gesture_capture_data = new List<Skeleton>();
        Gesture temp_gesture = new Gesture();
        ArrayList frame_rec_buffer = new ArrayList(); // use to store record frames in memory

        List<Button> buttons;
        static Button selected;

        float handX;
        float handY;


        public ExerciseWindow()
        {
            InitializeComponent();
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            //this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            toMeasureUnits = new List<MeasurementUnit>();
            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.kinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);
        }




        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
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
                    kinect_sensor = args.NewSensor;
                    kinect_data_manager = new KinectDataManager(ref kinect_sensor);
                    kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    kinect_sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
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
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }

                isStreaming = true;
                kinect_data_manager.ifShowJointStatus = true;

                frame_rec_buffer.Clear();

                kinect_sensor.Start();
            }
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
                //if (ifDoSmoothing)
                //{
                //    TransformSmoothParameters smoothingParam = new TransformSmoothParameters();
                //    {
                //        // Some smoothing with little latency (defaults).
                //        // Only filters out small jitters.
                //        // Good for gesture recognition in games.
                //        smoothingParam.Smoothing = 0.5f;
                //        smoothingParam.Correction = 0.5f;
                //        smoothingParam.Prediction = 0.5f;
                //        smoothingParam.JitterRadius = 0.05f;
                //        smoothingParam.MaxDeviationRadius = 0.04f;

                //        // Smoothed with some latency.
                //        // Filters out medium jitters.
                //        // Good for a menu system that needs to be smooth but
                //        // doesn't need the reduced latency as much as gesture recognition does.
                //        //smoothingParam.Smoothing = 0.5f;
                //        //smoothingParam.Correction = 0.1f;
                //        //smoothingParam.Prediction = 0.5f;
                //        //smoothingParam.JitterRadius = 0.1f;
                //        //smoothingParam.MaxDeviationRadius = 0.1f;

                //        //// Very smooth, but with a lot of latency.
                //        //// Filters out large jitters.
                //        //// Good for situations where smooth data is absolutely required
                //        //// and latency is not an issue.
                //        //smoothingParam.Smoothing = 0.7f;
                //        //smoothingParam.Correction = 0.3f;
                //        //smoothingParam.Prediction = 1.0f;
                //        //smoothingParam.JitterRadius = 1.0f;
                //        //smoothingParam.MaxDeviationRadius = 1.0f;
                //    };

                //    kinect_sensor.SkeletonStream.Enable(smoothingParam);
                //}
                //else
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


                return false;
            }

            isStreaming = true;
            kinect_data_manager.ifShowJointStatus = true;

            frame_rec_buffer.Clear();

            kinect_sensor.Start();

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

                gesture_capture_data.Add(tracked_skeleton);


                if (kinect_data_manager.ifShowJointStatus)
                {
                    // update status
                    motion_assessor.UpdateJointStatus(tracked_skeleton, toMeasureUnits);
                    kinect_data_manager.cur_joint_status = motion_assessor.GetCurrentJointStatus();
                    kinect_data_manager.toMeasureUnits = this.toMeasureUnits;
                    //Joint primaryHand = GetPrimaryHand(tracked_skeleton);
                    //TrackHand(primaryHand);
                    //ButtonPressed(tracked_skeleton);

                    // show feedback
                    //feedback_textblock.Text = motion_assessor.GetFeedbackForCurrentStatus();
                }

                kinect_data_manager.UpdateSkeletonData(tracked_skeleton);


            }
        }

        //track and display hand
        //private void TrackHand(Joint hand)
        //{
        //    if (hand.TrackingState == JointTrackingState.NotTracked)
        //    {
        //        kinectButton.Visibility = System.Windows.Visibility.Collapsed;
        //    }
        //    else
        //    {
        //        kinectButton.Visibility = System.Windows.Visibility.Visible;

        //        DepthImagePoint point = this.kinect_sensor.MapSkeletonPointToDepth(hand.Position, DepthImageFormat.Resolution640x480Fps30);
        //        handX = (int)((point.X * LayoutRoot.ActualWidth / this.kinect_sensor.DepthStream.FrameWidth) -
        //            (kinectButton.ActualWidth / 2.0));
        //        handY = (int)((point.Y * LayoutRoot.ActualHeight / this.kinect_sensor.DepthStream.FrameHeight) -
        //            (kinectButton.ActualHeight / 2.0));
        //        Canvas.SetLeft(kinectButton, handX);
        //        Canvas.SetTop(kinectButton, handY);

        //        if (isHandOver(kinectButton, buttons)) kinectButton.Hovering();
        //        else kinectButton.Release();
        //        if (hand.JointType == JointType.HandRight)
        //        {
        //            kinectButton.ImageSource = "/Images/RightHand.png";
        //            kinectButton.ActiveImageSource = "/Images/RightHand.png";
        //        }
        //        else
        //        {
        //            kinectButton.ImageSource = "/Images/LeftHand.png";
        //            kinectButton.ActiveImageSource = "/Images/LeftHand.png";
        //        }
        //    }
        //}

        //detect if hand is overlapping over any button
        private bool isHandOver(FrameworkElement hand, List<Button> buttonslist)
        {
            var handTopLeft = new System.Windows.Point(Canvas.GetLeft(hand), Canvas.GetTop(hand));
            var handX = handTopLeft.X + hand.ActualWidth / 2;
            var handY = handTopLeft.Y + hand.ActualHeight / 2;

            foreach (Button target in buttonslist)
            {
                System.Windows.Point targetTopLeft = new System.Windows.Point(Canvas.GetLeft(target), Canvas.GetTop(target));
                if (handX > targetTopLeft.X &&
                    handX < targetTopLeft.X + target.Width &&
                    handY > targetTopLeft.Y &&
                    handY < targetTopLeft.Y + target.Height)
                {
                    selected = target;
                    return true;
                }
            }
            return false;
        }

        //get the hand closest to the Kinect sensor
        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();
            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint rightHand = skeleton.Joints[JointType.HandRight];
                if (rightHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = rightHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > rightHand.Position.Z)
                        {
                            primaryHand = rightHand;
                        }
                    }
                }
            }
            return primaryHand;
        }

        //get the skeleton closest to the Kinect sensor
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;
            if (skeletons != null)
            {
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }
            return skeleton;
        }

        //private bool HitTest(Joint joint, UIElement target)
        //{
        //    return (GetHitTarget(joint, target) != null);
        //}

        //private IInputElement GetHitTarget(Joint joint, UIElement target)
        //{
        //    System.Windows.Point targetPoint = LayoutRoot.TranslatePoint(GetJointPoint(this.kinect_sensor, joint, LayoutRoot.RenderSize, new System.Windows.Point()), target);
        //    return target.InputHitTest(targetPoint);
        //}

        //private static System.Windows.Point GetJointPoint(KinectSensor kinectDevice, Joint joint, System.Windows.Size containerSize, System.Windows.Point offset)
        //{
        //    CoordinateMapper cm = new CoordinateMapper(kinectDevice);
        //    DepthImagePoint point = cm.MapSkeletonPointToDepthPoint(joint.Position, kinectDevice.DepthStream.Format);
        //    point.X = (int)((point.X * containerSize.Width / kinectDevice.DepthStream.FrameWidth) - offset.X);
        //    point.Y = (int)((point.Y * containerSize.Height / kinectDevice.DepthStream.FrameHeight) - offset.Y);

        //    return new System.Windows.Point(point.X, point.Y);
        //}

        //private void ButtonPressed(Skeleton skeleton)
        //{
        //    //Determine if the user triggers to start of a new game
        //    if (HitTest(skeleton.Joints[JointType.HandLeft], myButton) || HitTest(skeleton.Joints[JointType.HandRight], myButton))
        //    {
        //        DoubleAnimation a = new DoubleAnimation();
        //        a.To = 50;
        //        a.Duration = new Duration(TimeSpan.Parse("0:0:0.1"));

        //        //circle.BeginAnimation(System.Windows.Shapes.Ellipse.HeightProperty, a);
        //        //circle.BeginAnimation(System.Windows.Shapes.Ellipse.WidthProperty, a);
        //    }
        //    else
        //    {
        //        DoubleAnimation a = new DoubleAnimation();
        //        a.To = 40;
        //        a.Duration = new Duration(TimeSpan.Parse("0:0:0.1"));

        //        //circle.BeginAnimation(System.Windows.Shapes.Ellipse.HeightProperty, a);
        //        //circle.BeginAnimation(System.Windows.Shapes.Ellipse.WidthProperty, a);
        //    }
        //}



        /// <summary>
        /// Handle a button click from the wrap panel.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void KinectTileButtonClick(object sender, RoutedEventArgs e)
        {
            
            e.Handled = true;
        }

        private void RecordButtonClick(object sender, RoutedEventArgs e)
        {
            if (isRecording == true)
            {
                recordButtonImage.Source = new BitmapImage(new Uri("/KinectMotionAnalyzer;component/Resources/record_start.png", UriKind.Relative));
                isRecording = false;
            }
            else
            {
                recordButtonImage.Source = new BitmapImage(new Uri("/KinectMotionAnalyzer;component/Resources/record_end.png", UriKind.Relative));
                isRecording = true;
            }
            e.Handled = true;
        }

        private void AnglesButtonClick(object sender, RoutedEventArgs e)
        {
            var angleSelection = new AngleSelection(toMeasureUnits);
            this.LayoutRoot.Children.Add(angleSelection);
            e.Handled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect_sensor != null)
                kinect_sensor.Stop();
        }



        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // do initialization here
        //    if (!InitKinect())
        //    {
        //        //statusbarLabel.Content = "Kinect not connected";
        //        MessageBox.Show("Kinect not found.");
        //    }
        //    else
        //    {
        //        //statusbarLabel.Content = "Kinect initialized";
        //    }
        //}

        private void ske_disp_img_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {

        }


        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    message.Content = "Button 1 clicked!";
        //}

        //private void button2_Click(object sender, RoutedEventArgs e)
        //{
        //    message.Content = "Button 2 clicked!";
        //}
    }
}

        //<controls:KinectButtonFill x:Name="AngleButton" DisplayText="H" Canvas.Left="565" Canvas.Top="158" 
        //                       DisplayTextColor="Black" Time="1"  PathFill="Green" />