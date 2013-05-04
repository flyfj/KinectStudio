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
using System.Windows.Shapes;

namespace KinectMotionAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for EnterWindow.xaml
    /// </summary>
    public partial class EnterWindow : Window
    {
        public EnterWindow()
        {
            InitializeComponent();
        }

        private void mainEnterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (actionRecordingRadioBtn.IsChecked.Value)
            {
                MotionAnalyzerWindow_Trainer trainerWin = new MotionAnalyzerWindow_Trainer();
                trainerWin.ShowDialog();
            }

            if (FMSRadioBtn.IsChecked.Value)
            {
                MotionAnalyzerWindow_FMS fmsWin = new MotionAnalyzerWindow_FMS();
                fmsWin.ShowDialog();
            }

            if (realtimeRadioBtn.IsChecked.Value)
            {
                MotionAnalyzerWindow_User userWin = new MotionAnalyzerWindow_User();
                userWin.ShowDialog();
            }

            if (actionMatchingRadioBtn.IsChecked.Value)
            {
                DTWPreview dtwWin = new DTWPreview();
                dtwWin.ShowDialog();
            }
        }

    }
}
