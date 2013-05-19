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

        [Required]
        public string ActionName { get; set; }

        public string CurActionName { get; set; }

        //public ColorFrameData colorData { get; set; }
        public List<ColorFrameData> ColorFrames { get; set; }
        public List<DepthMapData> DepthFrames { get; set; }
        public List<SkeletonData> Skeletons { get; set; }

        public KinectAction()
        {
            ColorFrames = new List<ColorFrameData>();
            DepthFrames = new List<DepthMapData>();
            Skeletons = new List<SkeletonData>();
        }
    }

    // action category
    class ActionType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<float> JointWeights { get; set; }

    }
}
