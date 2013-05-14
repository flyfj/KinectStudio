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
    /// Interaction logic for AddActionWindow.xaml
    /// </summary>
    public partial class AddActionWindow : Window
    {
        public AddActionWindow()
        {
            InitializeComponent();
        }

        public string newActionName = "";

        private void addActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (newActionTextBox.Text == "")
            {
                MessageBox.Show("Input a valid action name.");
                return;
            }

            newActionName = newActionTextBox.Text;
            this.DialogResult = true;
        }


    }
}
