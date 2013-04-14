using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace KinectMotionAnalyzer.Model
{
    class MotionDBContext : DbContext
    {
        public DbSet<KinectAction> Actions { get; set; }
        public DbSet<ActionType> ActionTypes { get; set; }

        public MotionDBContext() : base("KinectMotionDB") { }
        public MotionDBContext(string dbname) : base(dbname) { }

    }
}
