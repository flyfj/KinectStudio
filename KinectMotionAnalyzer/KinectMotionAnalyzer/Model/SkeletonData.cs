using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Model
{
    class SkeletonData
    {
        [Key]
        public int Id { get; set; }

        public int Status { get; set; }

        public virtual List<SingleJoint> JointsData { get; set; }

        public KinectAction KAction { get; set; }
    }
}
