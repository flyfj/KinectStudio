namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removedepthandskeleton : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.KinectActions", "TestColor_Id", c => c.Int());
            AddColumn("dbo.ColorFrameDatas", "KinectAction_Id", c => c.Int());
            AddForeignKey("dbo.KinectActions", "TestColor_Id", "dbo.ColorFrameDatas", "Id");
            AddForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
            CreateIndex("dbo.KinectActions", "TestColor_Id");
            CreateIndex("dbo.ColorFrameDatas", "KinectAction_Id");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ColorFrameDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.KinectActions", new[] { "TestColor_Id" });
            DropForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.KinectActions", "TestColor_Id", "dbo.ColorFrameDatas");
            DropColumn("dbo.ColorFrameDatas", "KinectAction_Id");
            DropColumn("dbo.KinectActions", "TestColor_Id");
        }
    }
}
