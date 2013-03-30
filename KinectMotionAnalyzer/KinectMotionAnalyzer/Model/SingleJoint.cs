using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Model
{
    class SingleJoint
    {
        public int Id { get; set; }

        public int Type { get; set; }

        public SkeletonPoint Pos { get; set; }
    }
}
