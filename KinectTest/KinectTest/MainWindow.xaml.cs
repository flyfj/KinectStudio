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


namespace KinectTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinect_sensor;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
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
            if(kinect_sensor != null)
            {
                kinect_sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                kinect_sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                kinect_sensor.SkeletonStream.Enable();
                kinect_sensor.ColorStream.Enable(ColorImageFormat.InfraredResolution640x480Fps30);
            }

            MessageBox.Show("Kinect enabled.");

            // start kinect
            //kinect_sensor.Start();



        }
    }
}
