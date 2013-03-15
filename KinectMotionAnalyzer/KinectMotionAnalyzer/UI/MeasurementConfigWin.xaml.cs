using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.UI
{
    using KinectMotionAnalyzer.Processors;


    /// <summary>
    /// Interaction logic for MeasurementConfigWin.xaml
    /// </summary>
    public partial class MeasurementConfigWin : Window
    {

        private ArrayList joint_checkbox_collection = new ArrayList();
        private Dictionary<string, JointType> checkbox_name_jointtype_mapping = null;
        public List<MeasurementUnit> measureUnits = null;


        public MeasurementConfigWin()
        {
            InitializeComponent();
        }

        private void select_all_btn_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < joint_checkbox_collection.Count; i++)
            {
                (joint_checkbox_collection[i] as CheckBox).IsChecked = true;
            }
        }

        private void clear_all_btn_Click(object sender, RoutedEventArgs e)
        {
            // clear all checkbox
            for (int i = 0; i < joint_checkbox_collection.Count; i++)
            {
                (joint_checkbox_collection[i] as CheckBox).IsChecked = false;
            }
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            // check if any of the checkbox is clicked
            List<JointType> checkedJoints = new List<JointType>();
            for (int i = 0; i < joint_checkbox_collection.Count; i++)
            {
                CheckBox cur_checkbox = joint_checkbox_collection[i] as CheckBox;
                if (cur_checkbox.IsChecked.Value)
                    checkedJoints.Add(checkbox_name_jointtype_mapping[cur_checkbox.Name]);
            }

            if (checkedJoints.Count == 0)
            {
                MessageBox.Show("No joint selected");
                return;
            }
            else if (checkedJoints.Count == 1)
            {
                MeasurementUnit unit = new MeasurementUnit();
                unit.ifSingleJoint = true;
                unit.singleJoint = checkedJoints[0];
                measureUnits.Add(unit);

                // display on list
                measureUnitList.Items.Add(unit.singleJoint.ToString());
            }
            else if (checkedJoints.Count == 2)
            {
                MeasurementUnit unit = new MeasurementUnit();
                unit.ifSingleJoint = false;
                unit.boneJoint1 = checkedJoints[0];
                unit.boneJoint2 = checkedJoints[1];

                // check if plane is selected
                if (XYRadioBtn.IsChecked.Value)
                    unit.plane = PlaneName.XYPlane;
                else if (YZRadioBtn.IsChecked.Value)
                    unit.plane = PlaneName.YZPlane;
                else if (XZRadioBtn.IsChecked.Value)
                    unit.plane = PlaneName.XZPlane;
                else
                {
                    MessageBox.Show("Select plane first");
                    return;
                }

                // add to units
                measureUnits.Add(unit);

                // add to display list
                measureUnitList.Items.Add(
                    unit.boneJoint1.ToString() + " " +
                    unit.boneJoint2.ToString() + " " +
                    unit.plane.ToString());
            }
            else
                MessageBox.Show("Invalid selection. Only one or two joints are supported.");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            measureUnits = new List<MeasurementUnit>();

            // add all checkbox to collection
            joint_checkbox_collection.Add(head_checkbox);
            joint_checkbox_collection.Add(shoulder_center_checkbox);
            joint_checkbox_collection.Add(shoulder_left_checkbox);
            joint_checkbox_collection.Add(shoulder_right_checkbox);
            joint_checkbox_collection.Add(hand_left_checkbox);
            joint_checkbox_collection.Add(hand_right_checkbox);
            joint_checkbox_collection.Add(wrist_left_checkbox);
            joint_checkbox_collection.Add(wrist_right_checkbox);
            joint_checkbox_collection.Add(elbow_left_checkbox);
            joint_checkbox_collection.Add(elbow_right_checkbox);
            joint_checkbox_collection.Add(spine_checkbox);
            joint_checkbox_collection.Add(hip_center_checkbox);
            joint_checkbox_collection.Add(hip_left_checkbox);
            joint_checkbox_collection.Add(hip_right_checkbox);
            joint_checkbox_collection.Add(knee_left_checkbox);
            joint_checkbox_collection.Add(knee_right_checkbox);
            joint_checkbox_collection.Add(ankle_left_checkbox);
            joint_checkbox_collection.Add(ankle_right_checkbox);
            joint_checkbox_collection.Add(foot_left_checkbox);
            joint_checkbox_collection.Add(foot_right_checkbox);

            // set name type mapping
            checkbox_name_jointtype_mapping = new Dictionary<string, JointType>();
            checkbox_name_jointtype_mapping.Add(head_checkbox.Name, JointType.Head);
            checkbox_name_jointtype_mapping.Add(shoulder_center_checkbox.Name, JointType.ShoulderCenter);
            checkbox_name_jointtype_mapping.Add(shoulder_left_checkbox.Name, JointType.ShoulderLeft);
            checkbox_name_jointtype_mapping.Add(shoulder_right_checkbox.Name, JointType.ShoulderRight);
            checkbox_name_jointtype_mapping.Add(hand_left_checkbox.Name, JointType.HandLeft);
            checkbox_name_jointtype_mapping.Add(hand_right_checkbox.Name, JointType.HandRight);
            checkbox_name_jointtype_mapping.Add(wrist_left_checkbox.Name, JointType.WristLeft);
            checkbox_name_jointtype_mapping.Add(wrist_right_checkbox.Name, JointType.WristRight);
            checkbox_name_jointtype_mapping.Add(elbow_left_checkbox.Name, JointType.ElbowLeft);
            checkbox_name_jointtype_mapping.Add(elbow_right_checkbox.Name, JointType.ElbowRight);
            checkbox_name_jointtype_mapping.Add(spine_checkbox.Name, JointType.Spine);
            checkbox_name_jointtype_mapping.Add(hip_center_checkbox.Name, JointType.HipCenter);
            checkbox_name_jointtype_mapping.Add(hip_left_checkbox.Name, JointType.HipLeft);
            checkbox_name_jointtype_mapping.Add(hip_right_checkbox.Name, JointType.HipRight);
            checkbox_name_jointtype_mapping.Add(knee_left_checkbox.Name, JointType.KneeLeft);
            checkbox_name_jointtype_mapping.Add(knee_right_checkbox.Name, JointType.KneeRight);
            checkbox_name_jointtype_mapping.Add(ankle_left_checkbox.Name, JointType.AnkleLeft);
            checkbox_name_jointtype_mapping.Add(ankle_right_checkbox.Name, JointType.AnkleRight);
            checkbox_name_jointtype_mapping.Add(foot_left_checkbox.Name, JointType.FootLeft);
            checkbox_name_jointtype_mapping.Add(foot_right_checkbox.Name, JointType.FootRight);

        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        
    }
}
