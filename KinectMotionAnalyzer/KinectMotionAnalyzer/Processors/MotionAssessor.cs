using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using System.Diagnostics;


namespace KinectMotionAnalyzer.Processors
{

    public enum AxisName
    {
        XAxis,
        YAsix,
        ZAsix
    }

    public enum PlaneName
    {
        XYPlane,
        XZPlane,
        YZPlane
    }

    public class MeasurementUnit
    {
        public bool ifSingleJoint;
        
        // joint angle
        public JointType singleJoint;  

        // bone plane angle
        public JointType boneJoint1;
        public JointType boneJoint2;
        public PlaneName plane;

        public MeasurementUnit()
        {
            ifSingleJoint = true;
            singleJoint = JointType.ElbowRight;
        }
    }

    /// <summary>
    /// parameters associated with each joint at some time stamp
    /// </summary>
    public class JointStatus 
    {
        public Point3D position;
        public Point3D speed;

        public double abs_speed;   // absolute speed (m/s)
        public double angle;

        // angle for bone with certain axis (<180)
        // bone centered at current joint and connects to neighboring joint
        public Dictionary<JointType, Dictionary<AxisName, double>> axisAngles =
            new Dictionary<JointType, Dictionary<AxisName, double>>();
        public Dictionary<JointType, Dictionary<PlaneName, double>> planeAngles =
            new Dictionary<JointType, Dictionary<PlaneName, double>>();

        public JointStatus()
        {
            position = new Point3D(0, 0, 0);
            speed = new Point3D(0, 0, 0);
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


        private void ComputeJointAngles(Skeleton ske, JointType type, ref JointStatus status)
        {
            if (ske == null)
                return;

            SkeletonPoint cur_joint_pos = ske.Joints[type].Position;
            SkeletonPoint neighbor_joint_pos1 = new SkeletonPoint();
            SkeletonPoint neighbor_joint_pos2 = new SkeletonPoint();
            JointType neighbor_jointtype1 = new JointType();
            JointType neighbor_jointtype2 = new JointType();
            bool valid_joint = false;
            switch (type)   // specify which nearby joints are used to compute angle for current joint
            {
                case JointType.ElbowLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.WristLeft].Position;
                    neighbor_jointtype1 = JointType.WristLeft;
                    neighbor_joint_pos2 = ske.Joints[JointType.ShoulderLeft].Position;
                    neighbor_jointtype2 = JointType.ShoulderLeft;
                    valid_joint = true;
                    break;

                case JointType.ElbowRight:
                    neighbor_joint_pos1 = ske.Joints[JointType.WristRight].Position;
                    neighbor_jointtype1 = JointType.WristRight;
                    neighbor_joint_pos2 = ske.Joints[JointType.ShoulderRight].Position;
                    neighbor_jointtype2 = JointType.ShoulderRight;
                    valid_joint = true;
                    break;

                case JointType.KneeLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipLeft].Position;
                    neighbor_jointtype1 = JointType.HipLeft;
                    neighbor_joint_pos2 = ske.Joints[JointType.AnkleLeft].Position;
                    neighbor_jointtype2 = JointType.AnkleLeft;
                    valid_joint = true;
                    break;

                case JointType.KneeRight:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipRight].Position;
                    neighbor_jointtype1 = JointType.HipRight;
                    neighbor_joint_pos2 = ske.Joints[JointType.AnkleRight].Position;
                    neighbor_jointtype2 = JointType.AnkleRight;
                    valid_joint = true;
                    break;

                case JointType.ShoulderLeft:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipLeft].Position;
                    neighbor_jointtype1 = JointType.HipLeft;
                    neighbor_joint_pos2 = ske.Joints[JointType.ElbowLeft].Position;
                    neighbor_jointtype2 = JointType.ElbowLeft;
                    valid_joint = true;
                    break;

                case JointType.HipCenter:
                    neighbor_joint_pos1 = ske.Joints[JointType.HipRight].Position;
                    neighbor_jointtype1 = JointType.HipRight;
                    neighbor_joint_pos2 = ske.Joints[JointType.ShoulderCenter].Position;
                    neighbor_jointtype2 = JointType.ShoulderCenter;
                    valid_joint = true;
                    break;

                case JointType.Spine:
                    neighbor_joint_pos1 = ske.Joints[JointType.ShoulderCenter].Position;
                    neighbor_jointtype1 = JointType.ShoulderCenter;
                    neighbor_joint_pos2 = ske.Joints[JointType.HipCenter].Position;
                    neighbor_jointtype2 = JointType.HipCenter;
                    valid_joint = true;
                    break;
            }

            if (valid_joint)
            {
                // compute bone-bone angle
                Point3D vec1 = new Point3D(neighbor_joint_pos1.X - cur_joint_pos.X,
                        neighbor_joint_pos1.Y - cur_joint_pos.Y,
                        neighbor_joint_pos1.Z - cur_joint_pos.Z);
                Point3D vec2 = new Point3D(neighbor_joint_pos2.X - cur_joint_pos.X,
                            neighbor_joint_pos2.Y - cur_joint_pos.Y,
                            neighbor_joint_pos2.Z - cur_joint_pos.Z);

                status.angle = Tools.ComputeAngle(vec1, vec2);

                // add items
                // axis
                Dictionary<AxisName, double> axis_temp = new Dictionary<AxisName, double>();
                axis_temp.Add(AxisName.XAxis, 0);
                axis_temp.Add(AxisName.YAsix, 0);
                axis_temp.Add(AxisName.ZAsix, 0);
                if(!status.axisAngles.ContainsKey(neighbor_jointtype1))
                    status.axisAngles.Add(neighbor_jointtype1, axis_temp);
                if (!status.axisAngles.ContainsKey(neighbor_jointtype2))
                    status.axisAngles.Add(neighbor_jointtype2, axis_temp);

                // plane
                Dictionary<PlaneName, double> plane_temp = new Dictionary<PlaneName, double>();
                plane_temp.Add(PlaneName.XYPlane, 0);
                plane_temp.Add(PlaneName.YZPlane, 0);
                plane_temp.Add(PlaneName.XZPlane, 0);
                if (!status.planeAngles.ContainsKey(neighbor_jointtype1))
                    status.planeAngles.Add(neighbor_jointtype1, plane_temp);
                if (!status.planeAngles.ContainsKey(neighbor_jointtype2))
                    status.planeAngles.Add(neighbor_jointtype2, plane_temp);


                // compute axis angle
                Point3D xaxis = new Point3D(1, 0, 0);
                status.axisAngles[neighbor_jointtype1][AxisName.XAxis] = Tools.ComputeAngle(vec1, xaxis);
                status.axisAngles[neighbor_jointtype2][AxisName.XAxis] = Tools.ComputeAngle(vec2, xaxis);
                Point3D yaxis = new Point3D(0, 1, 0);
                status.axisAngles[neighbor_jointtype1][AxisName.YAsix] = Tools.ComputeAngle(vec1, yaxis);
                status.axisAngles[neighbor_jointtype2][AxisName.YAsix] = Tools.ComputeAngle(vec2, yaxis);
                Point3D zaxis = new Point3D(0, 0, 1);
                status.axisAngles[neighbor_jointtype1][AxisName.ZAsix] = Tools.ComputeAngle(vec1, zaxis);
                status.axisAngles[neighbor_jointtype2][AxisName.ZAsix] = Tools.ComputeAngle(vec2, zaxis);
                
                // compute plane angle
                Point3D xyplane1 = new Point3D(vec1.X, vec1.Y, 0);  // projection of vec1 to xy plane
                status.planeAngles[neighbor_jointtype1][PlaneName.XYPlane] = Tools.ComputeAngle(vec1, xyplane1);
                Point3D xyplane2 = new Point3D(vec2.X, vec2.Y, 0);
                status.planeAngles[neighbor_jointtype2][PlaneName.XYPlane] = Tools.ComputeAngle(vec2, xyplane2);
                Point3D yzplane1 = new Point3D(0, vec1.Y, vec1.Z);
                status.planeAngles[neighbor_jointtype1][PlaneName.YZPlane] = Tools.ComputeAngle(vec1, yzplane1);
                Point3D yzplane2 = new Point3D(0, vec2.Y, vec2.Z);
                status.planeAngles[neighbor_jointtype2][PlaneName.YZPlane] = Tools.ComputeAngle(vec2, yzplane2);
                Point3D xzplane1 = new Point3D(vec1.X, 0, vec1.Z);
                status.planeAngles[neighbor_jointtype1][PlaneName.XZPlane] = Tools.ComputeAngle(vec1, xzplane1);
                Point3D xzplane2 = new Point3D(vec2.X, 0, vec2.Z);
                status.planeAngles[neighbor_jointtype2][PlaneName.XZPlane] = Tools.ComputeAngle(vec2, xzplane2);

            }
            else
                status.angle = 0;
            

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
                ComputeJointAngles(ske, joint.JointType, ref stat);

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
            if (jointStatusSeq.Count == MAX_TRACK_LEN)
                jointStatusSeq.RemoveAt(0);

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
