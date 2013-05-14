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

namespace KinectMotionAnalyzer.UI.Controls
{
    /// <summary>
    /// Interaction logic for FMSSelectorView.xaml
    /// </summary>
    public partial class FMSSelectorView : UserControl
    {
        private KinectSensor kinect_sensor = null;


        public FMSSelectorView(KinectSensor kinect)
        {
            InitializeComponent();

            kinect_sensor = kinect;
        }

        private void KinectTileButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void mainGrid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.mainGrid.Visibility == Visibility.Hidden)
            {
                var parent = (Panel)this.Parent;
                parent.Children.Remove(this);
            }
        }



    }
}
