using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Diagnostics;


namespace KinectMotionAnalyzer.Processors
{

    /// <summary>
    /// parameters associated with each joint at some time stamp
    /// </summary>
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


    /// <summary>
    /// compute joint status given new skeleton at each frame
    /// </summary>
    public class MotionAssessor
    {
        // sequence of joint status
        public List<Dictionary<JointType, JointStatus>> jointStatusSeq = 
            new List<Dictionary<JointType, JointStatus>>();

        // maximum number of frames to track
        private int MAX_TRACK_LEN = 5;


        private double ComputeJointAngle(Skeleton ske, JointType type)
        {
            if (ske == null)
                return 0;

            SkeletonPoint cur_joint_pos = ske.Joints[type].Position;
            SkeletonPoint neighbor_joint_pos1 = new SkeletonPoint();
            SkeletonPoint neighbor_joint_pos2 = new SkeletonPoint();
            bool valid_joint = false;
            switch (type)   // specify which nearby joints are used to compute angle for current joint
            {
                case JointType.ElbowLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.WristLeft].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.ShoulderLeft].Position;
                    valid_joint = true;
                    break;

                case JointType.ElbowRight:
                    neighbor_joint_pos1 = ske.Joints[JointType.WristRight].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.ShoulderRight].Position;
                    valid_joint = true;
                    break;

                case JointType.KneeLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipLeft].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.AnkleLeft].Position;
                    valid_joint = true;
                    break;

                case JointType.KneeRight:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipRight].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.AnkleRight].Position;
                    valid_joint = true;
                    break;

                case JointType.ShoulderLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipLeft].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.ElbowLeft].Position;
                    valid_joint = true;
                    break;

                case JointType.Spine:
                    neighbor_joint_pos1 = ske.Joints[JointType.ShoulderCenter].Position;
                    neighbor_joint_pos2 = ske.Joints[JointType.HipCenter].Position;
                    valid_joint = true;
                    break;
            }

            if (valid_joint)
            {
                Point3D vec1 = new Point3D(neighbor_joint_pos1.X - cur_joint_pos.X,
                        neighbor_joint_pos1.Y - cur_joint_pos.Y,
                        neighbor_joint_pos1.Z - cur_joint_pos.Z);
                Point3D vec2 = new Point3D(neighbor_joint_pos2.X - cur_joint_pos.X,
                            neighbor_joint_pos2.Y - cur_joint_pos.Y,
                            neighbor_joint_pos2.Z - cur_joint_pos.Z);

                return Tools.ComputeAngle(vec1, vec2);
            }
            else
                return 0;
            

        }


        /// <summary>
        /// dummy test feedback
        /// </summary>
        /// <returns></returns>
        public string GetFeedbackForCurrentStatus()
        {
            // simple feedback test for back angle
            string res = "You are doing fine.";
            if (jointStatusSeq.Count > 0)
            {
                Dictionary<JointType, JointStatus> cur_joint_status =
                    jointStatusSeq[jointStatusSeq.Count - 1];
                // check back angle
                if (Math.Abs(cur_joint_status[JointType.Spine].angle - 180) > 10)
                    res = "Keep your back straight.";
            }
            
            return res;
        }


        /// <summary>
        /// given current skeleton, update status for each joint
        /// </summary>
        /// <param name="ske"></param>
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
                stat.angle = ComputeJointAngle(ske, joint.JointType);

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
            jointStatusSeq.Add(cur_joint_status);

        }


        /// <summary>
        /// getter
        /// </summary>
        public Dictionary<JointType, JointStatus> GetCurrentJointStatus()
        {
            if (jointStatusSeq.Count > 0)
                return jointStatusSeq[jointStatusSeq.Count - 1];
            else
                return null;
        }

    }
}
