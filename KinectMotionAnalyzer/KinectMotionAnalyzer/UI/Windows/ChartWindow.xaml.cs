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

        /// <summary>
        /// draw a normalized curve
        /// </summary>
        /// <param name="curveData"></param>
        private void DrawCurve(List<Point> curveData)
        {
            MainCanvas.Children.Clear();

            // margin for origin point
            Thickness graphMargin = new Thickness(30, 10, 10, 30);

            // actual canvas area
            Rect canvasBox = new Rect(0, 0, MainCanvas.ActualWidth, MainCanvas.ActualHeight);
            // origin point
            Point chartOrigin = new Point(graphMargin.Left, canvasBox.Height - graphMargin.Bottom);
            // axis
            Line axisVertical = new Line();
            axisVertical.Stroke = System.Windows.Media.Brushes.Black;
            axisVertical.StrokeThickness = 2;
            axisVertical.X1 = chartOrigin.X;
            axisVertical.Y1 = chartOrigin.Y;
            axisVertical.X2 = chartOrigin.X;
            axisVertical.Y2 = graphMargin.Top;
            Line axisHorizontal = new Line();
            axisHorizontal.Stroke = System.Windows.Media.Brushes.Black;
            axisHorizontal.StrokeThickness = 2;
            axisHorizontal.X1 = chartOrigin.X;
            axisHorizontal.Y1 = chartOrigin.Y;
            axisHorizontal.X2 = canvasBox.Width - graphMargin.Right;
            axisHorizontal.Y2 = chartOrigin.Y;

            MainCanvas.Children.Add(axisVertical);
            MainCanvas.Children.Add(axisHorizontal);

            // find maximum and minimum value of each coordinate


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
            Random randGenerator = new Random();
            List<Point> curve = new List<Point>();
            curve.Add(new Point(0, 0));
            for (int i = 1; i < 1000; i += 5)
            {
                int y = randGenerator.Next() % 400;
                curve.Add(new Point(i, y));
            }

            DrawCurve(curve);
        }


    }
}
