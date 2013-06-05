using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace KinectMotionAnalyzer.DataModel
{
    class MotionDBContext : DbContext
    {
        public DbSet<UserProfile> Users { get; set; }
        public DbSet<KinectAction> Actions { get; set; }
        public DbSet<ActionType> ActionTypes { get; set; }

        public MotionDBContext() : base("MotionDB") { }
        public MotionDBContext(string dbname) : base(dbname) { }

    }
}
