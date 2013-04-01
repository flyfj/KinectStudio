using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.Model
{
    class ColorFrameData
    {
        [Key]
        public int Id { get; set; }

        public int FrameId { get; set; }

        public int FrameWidth { get; set; }

        public int FrameHeight { get; set; }

        public byte[] FrameData { get; set; }
    }
}
