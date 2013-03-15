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
    /// Interaction logic for GestureConfigWin.xaml
    /// </summary>
    public partial class GestureConfigWin : Window
    {

        private ArrayList joint_checkbox_collection = new ArrayList();

        // used for outer access
        public GestureTemplateBase new_gesture_config = new GestureTemplateBase();


        public GestureConfigWin()
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

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            // add new gesture model
            if(new_gesture_name_box.Text == "")
            {
                MessageBox.Show("No gesture name.");
                return;
            }

            // create new gesture config
            new_gesture_config.name = new_gesture_name_box.Text;
            foreach (CheckBox box in joint_checkbox_collection)
            {
                if (box.IsChecked.Value)
                {
                    JointType type = (JointType)(int.Parse(box.Uid));
                    new_gesture_config.jointWeights[type] = 1;
                }
            }

            // close window
            this.DialogResult = true;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
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
        }
        
    }
}
