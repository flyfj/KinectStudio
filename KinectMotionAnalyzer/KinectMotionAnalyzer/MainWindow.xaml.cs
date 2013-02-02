using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;


namespace KinectMotionAnalyzer
{
    using KinectMotionAnalyzer.Processors;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectDataStreamManager kinect_data_manager;
        private KinectSensor kinect_sensor;


        public MainWindow()
        {
            InitializeComponent();

            if (!InitKinect())
            {
                kinectStatusLabel.Content = "Kinect not connected";
                MessageBox.Show("Kinect not found.");
            }
            else
                kinectStatusLabel.Content = "Kinect initialized";



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
                kinect_data_manager = new KinectDataStreamManager(ref kinect_sensor);

            // enable data stream
            if (kinect_sensor != null)
            {
                // initialize all streams
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                kinect_sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                kinect_sensor.SkeletonStream.Enable();
                // can't use IR simultaneously with color!
                //kinect_sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);

                // initialize image sources
                kinect_data_manager.ColorStreamBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);

                kinect_data_manager.DepthStreamBitmap = new WriteableBitmap(
                    kinect_sensor.DepthStream.FrameWidth, kinect_sensor.DepthStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);

                // set source (must after source has been initialized otherwise it's null forever)
                color_disp_img.Source = kinect_data_manager.ColorStreamBitmap;
                depth_disp_img.Source = kinect_data_manager.DepthStreamBitmap;
                skeleton_disp_img.Source = kinect_data_manager.skeletonImageSource;

                // bind event handlers
                kinect_sensor.ColorFrameReady += kinect_colorframe_ready;
                kinect_sensor.DepthFrameReady += kinect_depthframe_ready;
                kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;

                // enable data stream based on initial check
                if (!colorCheckBox.IsChecked.Value)
                    kinect_sensor.ColorStream.Disable();
                if (!depthCheckBox.IsChecked.Value)
                    kinect_sensor.DepthStream.Disable();
                if (!skeletonCheckBox.IsChecked.Value)
                    kinect_sensor.SkeletonStream.Disable();

            }
            else
                return false;


            return true;
        }


#region data_ready_handlers

        void kinect_colorframe_ready(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateColorData(frame);
            }
        }

        void kinect_depthframe_ready(object sender, DepthImageFrameReadyEventArgs e)
        {

            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateDepthData(frame);
            }
        }

        void kinect_skeletonframe_ready(object sender, SkeletonFrameReadyEventArgs e)
        {

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateSkeletonData(frame);
            }
        }

#endregion
        

#region UI_control

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {

            if (kinect_sensor != null)
            {
                if (!kinect_sensor.IsRunning)
                {
                    kinect_sensor.Start();
                    runBtn.Content = "Stop Kinect";
                }
                else
                {
                    kinect_sensor.Stop();
                    runBtn.Content = "Start Kinect";
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect_sensor != null)
            {
                kinect_sensor.Stop();
            }
        }

        private void colorCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!kinect_sensor.ColorStream.IsEnabled)
                kinect_sensor.ColorStream.Enable();
        }

        private void colorCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (kinect_sensor.ColorStream.IsEnabled)
                kinect_sensor.ColorStream.Disable();
        }

        private void depthCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!kinect_sensor.DepthStream.IsEnabled)
                kinect_sensor.DepthStream.Enable();
        }

        private void depthCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (kinect_sensor.DepthStream.IsEnabled)
                kinect_sensor.DepthStream.Disable();
        }

        private void skeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (!kinect_sensor.SkeletonStream.IsEnabled)
                kinect_sensor.SkeletonStream.Enable();
        }

        private void skeletonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            if (kinect_sensor.SkeletonStream.IsEnabled)
                kinect_sensor.SkeletonStream.Disable();
        }
        

#endregion

        /// <summary>
        /// Save current kinect data
        /// </summary>
        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (kinect_sensor == null || !kinect_sensor.IsRunning)
                return;


            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = "D:"; //Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string colorpath = myPhotos + "\\Kinect_color_" + time + ".png";
            string depthpath = myPhotos + "\\Kinect_depth_" + time + ".txt";
            string skeletonpath = myPhotos + "\\Kinect_skeleton_" + time + ".txt";

            if (kinect_sensor.ColorStream.IsEnabled)
                kinect_data_manager.SaveKinectData(kinect_data_manager.ColorStreamBitmap, colorpath, "COLOR");
            if (kinect_sensor.DepthStream.IsEnabled)
                kinect_data_manager.SaveKinectData(kinect_data_manager.depthPixels, depthpath, "DEPTH");
            if (kinect_sensor.SkeletonStream.IsEnabled)
                kinect_data_manager.SaveKinectData(kinect_data_manager.skeletons, skeletonpath, "SKELETON");
            
        }


    }
}
