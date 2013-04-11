using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.Model
{
    // each basic exercise, e.g. one repetition
    class KinectAction
    {
        [Key]
        public int Id { get; set; }

        public string ActionName { get; set; }

        public List<ColorFrameData> ColorFrames { get; set; }
        public List<DepthMapData> DepthFrames { get; set; }
        public List<SkeletonData> Skeletons { get; set; }
    }
}
