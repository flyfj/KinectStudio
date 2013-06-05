using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.DataModel
{
    class ColorFrameData
    {
        [Key]
        public int Id { get; set; }

        public int FrameId { get; set; }

        [Required]
        public int FrameWidth { get; set; }

        [Required]
        public int FrameHeight { get; set; }

        [Column(TypeName = "image")]
        public byte[] FrameData { get; set; }

        //[Required]
       // public int KinectActionId { get; set; }
    }
}
