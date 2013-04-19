using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.Model
{
    class DepthMapData
    {
        [Key]
        public int Id { get; set; }

        public int FrameId { get; set; }

        public int FrameWidth { get; set; }

        public int FrameHeight { get; set; }

        //[Column(TypeName = "image")]
        //public short[] DepthData { get; set; }

        //public KinectAction KAction { get; set; }
    }
}
