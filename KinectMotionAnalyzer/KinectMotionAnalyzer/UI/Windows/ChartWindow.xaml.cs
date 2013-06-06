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

namespace KinectMotionAnalyzer.UI.Windows
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public ChartWindow()
        {
            InitializeComponent();
        }

        private void DrawCurve(List<Point> curveData)
        {
            MainCanvas.Children.Clear();

            Point chartOrigin = new Point(10, MainCanvas.ActualHeight - 10);
            for (int i = 1; i < curveData.Count; i++)
            {
                Line myLine = new Line();
                myLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
                myLine.X1 = chartOrigin.X + curveData[i - 1].X;
                myLine.Y1 = chartOrigin.Y - curveData[i - 1].Y;
                myLine.X2 = chartOrigin.X + curveData[i].X;
                myLine.Y2 = chartOrigin.Y - curveData[i].Y;
                myLine.HorizontalAlignment = HorizontalAlignment.Left;
                myLine.VerticalAlignment = VerticalAlignment.Center;
                myLine.StrokeThickness = 2;
                MainCanvas.Children.Add(myLine);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<Point> curve = new List<Point>();
            curve.Add(new Point(0, 0));
            curve.Add(new Point(50, 50));
            curve.Add(new Point(70, 40));
            curve.Add(new Point(80, 80));

            DrawCurve(curve);
        }


    }
}
