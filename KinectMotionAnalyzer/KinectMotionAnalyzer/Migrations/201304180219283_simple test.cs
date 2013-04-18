namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class simpletest : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ColorFrameDatas", "KinectActionId", "dbo.KinectActions");
            DropIndex("dbo.ColorFrameDatas", new[] { "KinectActionId" });
            AddColumn("dbo.KinectActions", "colorData_Id", c => c.Int());
            AddForeignKey("dbo.KinectActions", "colorData_Id", "dbo.ColorFrameDatas", "Id");
            CreateIndex("dbo.KinectActions", "colorData_Id");
            DropColumn("dbo.ColorFrameDatas", "KinectActionId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ColorFrameDatas", "KinectActionId", c => c.Int(nullable: false));
            DropIndex("dbo.KinectActions", new[] { "colorData_Id" });
            DropForeignKey("dbo.KinectActions", "colorData_Id", "dbo.ColorFrameDatas");
            DropColumn("dbo.KinectActions", "colorData_Id");
            CreateIndex("dbo.ColorFrameDatas", "KinectActionId");
            AddForeignKey("dbo.ColorFrameDatas", "KinectActionId", "dbo.KinectActions", "Id", cascadeDelete: true);
        }
    }
}
