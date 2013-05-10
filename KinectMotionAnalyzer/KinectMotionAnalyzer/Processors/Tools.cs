using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
using KinectMotionAnalyzer.Model;
using System.Windows;


namespace KinectMotionAnalyzer.Processors
{

    /// <summary>
    /// common functions
    /// </summary>
    class Tools
    {

        static public float GetJointDistance(SkeletonPoint a, SkeletonPoint b)
        {
            float dist = 0;
            dist += (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.X - b.Z) * (a.X - b.Z);
            dist = (float)Math.Sqrt((double)dist);

            return dist;
        }

        /// <summary>
        /// L2 norm
        /// </summary>
        static public double Dist2(double[] a, double[] b)
        {
            double dist = 0;
            for(int i=0; i<a.Length; i++)
                dist += Math.Pow(a[i] - b[i], 2);

            return Math.Sqrt(dist);
        }

        /// <summary>
        /// compute angle between vectors
        /// </summary>
        /// <returns>degree between 0 and 180</returns>
        static public double ComputeAngle(Point3D vec1, Point3D vec2)
        {
            double val = vec1.X * vec2.X + vec1.Y * vec2.Y + vec1.Z * vec2.Z;
            double vec1_norm = Math.Sqrt(vec1.X * vec1.X + vec1.Y * vec1.Y + vec1.Z * vec1.Z);
            double vec2_norm = Math.Sqrt(vec2.X * vec2.X + vec2.Y * vec2.Y + vec2.Z * vec2.Z);
            double angle = Math.Acos(val / (vec1_norm * vec2_norm));

            return angle * 180 / Math.PI;
        }

        static public bool ConvertFromKinectAction(KinectAction action,
            out List<byte[]> color_frames,
            out List<DepthImagePixel[]> depth_frames,
            out List<Skeleton> skeleton_buffer)
        {
            if (action.ColorFrames == null)
            {
                // clear
                color_frames = new List<byte[]>();
                depth_frames = new List<DepthImagePixel[]>();
                skeleton_buffer = new List<Skeleton>();
                return false;
            }

            // color frames
            color_frames = new List<byte[]>();
            foreach (ColorFrameData colorData in action.ColorFrames)
            {
                color_frames.Add(colorData.FrameData);
            }
            // skeletons
            skeleton_buffer = new List<Skeleton>();
            foreach (SkeletonData skeData in action.Skeletons)
            {
                if (skeData == null)
                {
                    skeleton_buffer.Add(null);
                    continue;
                }

                Skeleton cur_ske = new Skeleton();
                cur_ske.TrackingState = (SkeletonTrackingState)skeData.Status;
                foreach (SingleJoint joint in skeData.JointsData)
                {
                    JointType cur_type = (JointType)joint.Type;
                    // get joint object
                    Joint cur_joint = cur_ske.Joints[cur_type];
                    SkeletonPoint point = new SkeletonPoint();
                    point.X = joint.PosX;
                    point.Y = joint.PosY;
                    point.Z = joint.PosZ;
                    cur_joint.Position = point;
                    cur_joint.TrackingState = (JointTrackingState)joint.TrackingStatus;
                    cur_ske.Joints[cur_type] = cur_joint;
                }
                skeleton_buffer.Add(cur_ske);
            }
            // depth image
            depth_frames = new List<DepthImagePixel[]>();
            //foreach (DepthMapData dData in action.DepthFrames)
            //{
            //    DepthImagePixel[] depthPixels = new DepthImagePixel[dData.DepthData.Length];
            //    for (int i = 0; i < dData.DepthData.Length; i++ )
            //    {
            //        depthPixels[i].Depth = dData.DepthData[i];
            //    }
            //}

            return true;
        }

        static public void AlignSkeletons(Skeleton inputSke, Skeleton targetSke)
        {
            // align input to target
            
            // get input scale
            Point shoulderRight = new Point(
                inputSke.Joints[JointType.ShoulderRight].Position.X,
                inputSke.Joints[JointType.ShoulderRight].Position.Y);
            Point shoulderLeft = new Point(
                inputSke.Joints[JointType.ShoulderLeft].Position.X,
                inputSke.Joints[JointType.ShoulderLeft].Position.Y);
            double inputShoulderDist =
               Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                         Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));

            // get target scale
            shoulderRight = new Point(
                targetSke.Joints[JointType.ShoulderRight].Position.X,
                targetSke.Joints[JointType.ShoulderRight].Position.Y);
            shoulderLeft = new Point(
                targetSke.Joints[JointType.ShoulderLeft].Position.X,
                targetSke.Joints[JointType.ShoulderLeft].Position.Y);
            double targetShoulderDist =
               Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                         Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));

            // Center the data
            var center = new Point(
                targetSke.Joints[JointType.ShoulderCenter].Position.X,
                targetSke.Joints[JointType.ShoulderCenter].Position.Y);
            for (int k = 0; k < pts.Count; k++)
                pts[k] = new Point(pts[k].X - center.X, pts[k].Y - center.Y);

            // Normalization of the coordinates
           
            for (int k = 0; k < pts.Count; k++)
                pts[k] = new Point(pts[k].X / shoulderDist, pts[k].Y / shoulderDist);
        }
    }
}
