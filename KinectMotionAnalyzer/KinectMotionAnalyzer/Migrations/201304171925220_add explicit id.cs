namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addexplicitid : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions");
            DropIndex("dbo.ColorFrameDatas", new[] { "KinectAction_Id" });
            RenameColumn(table: "dbo.ColorFrameDatas", name: "KinectAction_Id", newName: "KinectActionId");
            AddForeignKey("dbo.ColorFrameDatas", "KinectActionId", "dbo.KinectActions", "Id", cascadeDelete: true);
            CreateIndex("dbo.ColorFrameDatas", "KinectActionId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ColorFrameDatas", new[] { "KinectActionId" });
            DropForeignKey("dbo.ColorFrameDatas", "KinectActionId", "dbo.KinectActions");
            RenameColumn(table: "dbo.ColorFrameDatas", name: "KinectActionId", newName: "KinectAction_Id");
            CreateIndex("dbo.ColorFrameDatas", "KinectAction_Id");
            AddForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
        }
    }
}
