using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.DataModel
{
    class SingleJoint
    {
        [Key]
        public int Id { get; set; }

        public int Type { get; set; }

        public float PosX { get; set; }

        public float PosY { get; set; }

        public float PosZ { get; set; }

        public int TrackingStatus { get; set; }

        //public SkeletonData SkeData { get; set; }
    }
}
