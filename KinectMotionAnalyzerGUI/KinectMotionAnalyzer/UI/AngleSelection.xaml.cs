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
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect;
using System.Collections;
using KinectMotionAnalyzer.Processors;

namespace KinectMotionAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for AngleSelection.xaml
    /// </summary>
    public partial class AngleSelection : UserControl
    {
        private readonly Brush clickedButtonBrush = Brushes.Blue;
        private readonly Brush unClickedButtonBrush = Brushes.Black;

        private ArrayList planeButtons = new ArrayList();
        private ArrayList jointButtons = new ArrayList();
        private Dictionary<string, JointType> jointtype_mapping = null;
        public List<MeasurementUnit> measureUnits = null;
        public MeasurementUnit unit = null;
        private int jointCount = 0;

        public AngleSelection(List<MeasurementUnit> toMeasureUnits)
        {
            InitializeComponent();
            this.measureUnits = toMeasureUnits;
        }



        private void JointButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (KinectCircleButton)e.OriginalSource;

            if (button.Foreground == clickedButtonBrush)
            {
                int i = jointButtons.IndexOf(button);
                (jointButtons[i] as KinectCircleButton).Foreground = unClickedButtonBrush;
                jointButtons.RemoveAt(i);
                jointCount = i;
            }
            else
            {
                button.Foreground = clickedButtonBrush;

                if (jointCount > 1)
                {
                    (jointButtons[jointCount % 2] as KinectCircleButton).Foreground = unClickedButtonBrush;
                    jointButtons.RemoveAt(jointCount % 2);
                }
                jointButtons.Insert(jointCount % 2, button);
                jointCount++;
            }
            e.Handled = true;
        }

        private void PlaneButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (KinectCircleButton)e.OriginalSource;

            for (int i = 0; i < planeButtons.Count; i++)
            {
                (planeButtons[i] as KinectCircleButton).Foreground = unClickedButtonBrush;
            }

            // check if plane is selected
            if (button.Name == "XY")
                unit.plane = PlaneName.XYPlane;
            else if (button.Name == "YZ")
                unit.plane = PlaneName.YZPlane;
            else // button.Name == "XZ"
                unit.plane = PlaneName.XZPlane;

            button.Foreground = clickedButtonBrush;

            e.Handled = true;
        }

        private void DoneButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (KinectCircleButton)e.OriginalSource;

            if (jointButtons.Count == 1 && unit.plane == PlaneName.None)
            {
                unit.ifSingleJoint = true;
                unit.singleJoint = jointtype_mapping[(jointButtons[0] as KinectCircleButton).Name];
                measureUnits.Add(unit);
            }
            else if (jointButtons.Count == 2 && unit.plane != PlaneName.None)
            {
                unit.ifSingleJoint = false;
                unit.boneJoint1 = jointtype_mapping[(jointButtons[0] as KinectCircleButton).Name];
                unit.boneJoint2 = jointtype_mapping[(jointButtons[1] as KinectCircleButton).Name];
                measureUnits.Add(unit);
            }
            else if (jointButtons.Count == 0 && unit.plane == PlaneName.None)
            {
                //do Nothing
            }
            else
            {
                NotifyText.Text = "Invalid Angle";
                return;
            }

            var parent = (Panel)this.Parent;
            parent.Children.Remove(this);


            e.Handled = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            unit = new MeasurementUnit();

            // set name type mapping
            jointtype_mapping = new Dictionary<string, JointType>();
            jointtype_mapping.Add(Head.Name, JointType.Head);
            jointtype_mapping.Add(ShoulderCenter.Name, JointType.ShoulderCenter);
            jointtype_mapping.Add(ShoulderLeft.Name, JointType.ShoulderLeft);
            jointtype_mapping.Add(ShoulderRight.Name, JointType.ShoulderRight);
            jointtype_mapping.Add(HandLeft.Name, JointType.HandLeft);
            jointtype_mapping.Add(HandRight.Name, JointType.HandRight);
            jointtype_mapping.Add(WristLeft.Name, JointType.WristLeft);
            jointtype_mapping.Add(WristRight.Name, JointType.WristRight);
            jointtype_mapping.Add(ElbowLeft.Name, JointType.ElbowLeft);
            jointtype_mapping.Add(ElbowRight.Name, JointType.ElbowRight);
            jointtype_mapping.Add(Spine.Name, JointType.Spine);
            jointtype_mapping.Add(HipCenter.Name, JointType.HipCenter);
            jointtype_mapping.Add(HipLeft.Name, JointType.HipLeft);
            jointtype_mapping.Add(HipRight.Name, JointType.HipRight);
            jointtype_mapping.Add(KneeLeft.Name, JointType.KneeLeft);
            jointtype_mapping.Add(KneeRight.Name, JointType.KneeRight);
            jointtype_mapping.Add(AnkleLeft.Name, JointType.AnkleLeft);
            jointtype_mapping.Add(AnkleRight.Name, JointType.AnkleRight);
            jointtype_mapping.Add(FootLeft.Name, JointType.FootLeft);
            jointtype_mapping.Add(FootRight.Name, JointType.FootRight);

            planeButtons.Add(XY);
            planeButtons.Add(YZ);
            planeButtons.Add(XZ);
        }


        
    }
}
