using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Model
{
    class SkeletonData
    {
        public int Id { get; set; }

        public int Status { get; set; }

        public List<SingleJoint> JointsData { get; set; }
    }
}
