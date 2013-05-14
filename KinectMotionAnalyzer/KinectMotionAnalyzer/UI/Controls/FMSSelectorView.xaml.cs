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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;

namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic for FMSSelectorView.xaml
    /// </summary>
    public partial class FMSSelectorView : UserControl
    {

        private readonly KinectSensorChooser sensorChooser = null;
        private readonly MainUserWindow parentWindow = null;


        public FMSSelectorView(KinectSensorChooser chooser, MainUserWindow parentWin)
        {
            InitializeComponent();

            sensorChooser = chooser;
            parentWindow = parentWin;
        }

        private void KinectTileButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (KinectTileButton)e.OriginalSource;
            string caption = button.Label as string;
            if (caption != "Exit")
            {
                FMSProcessorView fmsProcessorView = new FMSProcessorView(sensorChooser, parentWindow);
                parentWindow.holderGrid.Children.Add(fmsProcessorView);

                // stop parent kinect region
                parentWindow.kinectRegion.IsEnabled = false;
            }
            else
            {
                (this.Parent as Panel).Children.Remove(this);
            }

        }

        private void mainGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.mainGrid.Visibility == Visibility.Hidden)
            {
                var parent = (Panel)this.Parent;
                parent.Children.Remove(this);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
        }



    }
}
