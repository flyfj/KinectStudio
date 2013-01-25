using System;
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

            kinect_data_manager = new KinectDataStreamManager();

            if (!InitKinect())
                MessageBox.Show("Kinect not connected.");
            else
                MessageBox.Show("Kinect initialized.");
        }

  
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
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                kinect_data_manager.StreamDataBitmap = new WriteableBitmap(
                    kinect_sensor.ColorStream.FrameWidth, kinect_sensor.ColorStream.FrameHeight, 96, 96,
                    PixelFormats.Bgr32, null);

                // set source (must after source has been initialized otherwise it's null forever)
                display_image.Source = kinect_data_manager.StreamDataBitmap;

                //kinect_sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                //kinect_sensor.SkeletonStream.Enable();

                // can't use IR simultaneously with color
                //kinect_sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
            }
            else
                return false;

            kinect_sensor.ColorFrameReady += kinect_colorframe_ready;
            kinect_sensor.DepthFrameReady += kinect_depthframe_ready;
            kinect_sensor.SkeletonFrameReady += kinect_skeletonframe_ready;

            return true;
        }


        // data ready handlers
        void kinect_colorframe_ready(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (streamCombBox.SelectedIndex != 0)
                return;

            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateColorData(frame);
            }
        }

        void kinect_depthframe_ready(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (streamCombBox.SelectedIndex != 1)
                return;

            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateDepthData(frame);
            }
        }

        void kinect_skeletonframe_ready(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (streamCombBox.SelectedIndex != 2)
                return;

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                kinect_data_manager.UpdateSkeletonData(frame);
            }
        }


        //
        private void runBtn_Click(object sender, RoutedEventArgs e)
        {
            if(runBtn.Content.ToString() == "Start Kinect")
            {
                if (kinect_sensor != null)
                {
                    kinect_sensor.Start();
                    runBtn.Content = "Stop Kinect";
                }
            }
            else
            {
                if (kinect_sensor != null)
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

        private void streamCombBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (kinect_sensor == null)
                return;

            // stop current streams
            kinect_sensor.ColorStream.Disable();
            kinect_sensor.DepthStream.Disable();
            kinect_sensor.SkeletonStream.Disable();
            
            // start selected stream
            int cur_sel = (sender as ComboBox).SelectedIndex;
            if (cur_sel == 0)
                kinect_sensor.ColorStream.Enable();
            if (cur_sel == 1)
                kinect_sensor.DepthStream.Enable();
            if (cur_sel == 2)
                kinect_sensor.SkeletonStream.Enable();    

        }





    }
}
