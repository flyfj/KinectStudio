using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.Model
{
    // each basic exercise, e.g. one repetition
    class KinectAction
    {
        public int Id { get; set; }

        public string ActionName { get; set; }

        public List<ColorFrameData> ColorFrames { get; set; }
        public List<SkeletonData> Skeletons { get; set; }
    }
}
