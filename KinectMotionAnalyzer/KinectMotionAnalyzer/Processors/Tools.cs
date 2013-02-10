using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Processors
{
    class Tools
    {
        static public float GetJointDistance(SkeletonPoint a, SkeletonPoint b)
        {
            float dist = 0;
            dist += (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.X - b.Z) * (a.X - b.Z);
            dist = (float)Math.Sqrt((double)dist);

            return dist;
        }
    }
}
