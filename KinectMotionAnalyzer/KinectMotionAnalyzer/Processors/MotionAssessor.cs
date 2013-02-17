using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Diagnostics;


namespace KinectMotionAnalyzer.Processors
{

    public class JointStatus 
    {
        public Point3D position;
        public Point3D speed;
        public Point3D axis_angle;  // angles with x, y, z axis in 3d

        public double abs_speed;   // absolute speed (m/s)
        public double angle;


        public JointStatus()
        {
            position = new Point3D(0, 0, 0);
            speed = new Point3D(0, 0, 0);
            axis_angle = new Point3D(0, 0, 0);
            abs_speed = 0;
            angle = 0;
        }
    }

    public class MotionAssessor
    {
        // sequence of joint status
        private List<Dictionary<JointType, JointStatus>> jointStatusSeq = 
            new List<Dictionary<JointType, JointStatus>>();

        // maximum number of frames to track
        private int MAX_TRACK_LEN = 5;

        /// <summary>
        /// compute angle between vectors
        /// </summary>
        /// <returns>degree between 0 and 180</returns>
        private double ComputeAngle(Point3D vec1, Point3D vec2)
        {
            double val = vec1.X * vec2.X + vec1.Y * vec2.Y + vec1.Z * vec2.Z;
            double vec1_norm = Math.Sqrt(vec1.X * vec1.X + vec1.Y * vec1.Y + vec1.Z * vec1.Z);
            double vec2_norm = Math.Sqrt(vec2.X * vec2.X + vec2.Y * vec2.Y + vec2.Z * vec2.Z);
            double angle = Math.Acos(val / (vec1_norm * vec2_norm));

            return angle * 180 / Math.PI;
        }


        public void UpdateJointStatus(Skeleton ske)
        {
            if (ske == null)
            {
                // gradually remove buffer
                if (jointStatusSeq.Count > 0)
                    jointStatusSeq.RemoveAt(0);

                return;
            }

            if (ske.TrackingState != SkeletonTrackingState.Tracked)
            {
                Debug.WriteLine("Input skeleton not tracked.");
                return;
            }

            // extract information for each joint
            Dictionary<JointType, JointStatus> cur_joint_status = new Dictionary<JointType, JointStatus>();
            foreach (Joint joint in ske.Joints)
            {
                JointStatus stat = new JointStatus();
                // position
                stat.position.X = joint.Position.X;
                stat.position.Y = joint.Position.Y;
                stat.position.Z = joint.Position.Z;
                // angle
                switch (joint.JointType)
                {
                    case JointType.ElbowLeft:

                        // use wrist left and shoulder left bones
                        SkeletonPoint wrist_left = ske.Joints[JointType.WristLeft].Position;
                        SkeletonPoint shoulder_left = ske.Joints[JointType.ShoulderLeft].Position;
                        Point3D elbow2wrist_vec = new Point3D(wrist_left.X - joint.Position.X,
                            wrist_left.Y - joint.Position.Y, wrist_left.Z - joint.Position.Z);
                        Point3D elbow2shoulder_vec = new Point3D(shoulder_left.X - joint.Position.X,
                            shoulder_left.Y - joint.Position.Y, shoulder_left.Z - joint.Position.Z);
                        stat.angle = ComputeAngle(elbow2shoulder_vec, elbow2wrist_vec);
                        break;

                    case JointType.ElbowRight:
                        break;
                }
                // compute speed using last frame data
                if(jointStatusSeq.Count > 0)
                {
                    stat.speed.X = stat.position.X - 
                        jointStatusSeq[jointStatusSeq.Count - 1][joint.JointType].position.X;
                    stat.speed.Y = stat.position.Y -
                        jointStatusSeq[jointStatusSeq.Count - 1][joint.JointType].position.Y;
                    stat.speed.Z = stat.position.Z -
                        jointStatusSeq[jointStatusSeq.Count - 1][joint.JointType].position.Z;

                    stat.abs_speed = 30 * Math.Sqrt(Math.Pow(stat.speed.X, 2) +
                        Math.Pow(stat.speed.Y, 2) + Math.Pow(stat.speed.Z, 2));
                }

                // add to dict
                cur_joint_status.Add(joint.JointType, stat);

            }

            // add to sequence
            if (jointStatusSeq.Count < MAX_TRACK_LEN)
                jointStatusSeq.Add(cur_joint_status);
            else
            {
                jointStatusSeq.RemoveAt(0);
                jointStatusSeq.Add(cur_joint_status);
            }

        }

        public Dictionary<JointType, JointStatus> GetCurrentJointStatus()
        {
            if (jointStatusSeq.Count > 0)
                return jointStatusSeq[jointStatusSeq.Count - 1];
            else
                return null;
        }

    }
}
