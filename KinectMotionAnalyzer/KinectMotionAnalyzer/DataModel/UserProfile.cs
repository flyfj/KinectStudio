using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMotionAnalyzer.DataModel
{
    enum UserType
    {
        USER_TYPE_CLIENT,
        USER_TYPE_TRAINER
    }

    class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public UserType Identity { get; set; }
    }
}
