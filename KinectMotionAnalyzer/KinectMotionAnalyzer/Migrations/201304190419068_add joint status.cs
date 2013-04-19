namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addjointstatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SingleJoints", "TrackingStatus", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SingleJoints", "TrackingStatus");
        }
    }
}
