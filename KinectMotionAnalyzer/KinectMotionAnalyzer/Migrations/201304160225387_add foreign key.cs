namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addforeignkey : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.DepthMapDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.SkeletonDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.SingleJoints", "SkeletonData_Id", "dbo.SkeletonDatas");
            DropIndex("dbo.ColorFrameDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.DepthMapDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.SkeletonDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.SingleJoints", new[] { "SkeletonData_Id" });
            AddColumn("dbo.ColorFrameDatas", "KAction_Id", c => c.Int());
            AddColumn("dbo.DepthMapDatas", "KAction_Id", c => c.Int());
            AddColumn("dbo.SkeletonDatas", "KAction_Id", c => c.Int());
            AddColumn("dbo.SingleJoints", "SkeData_Id", c => c.Int());
            AddForeignKey("dbo.ColorFrameDatas", "KAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.DepthMapDatas", "KAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.SkeletonDatas", "KAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.SingleJoints", "SkeData_Id", "dbo.SkeletonDatas", "Id");
            CreateIndex("dbo.ColorFrameDatas", "KAction_Id");
            CreateIndex("dbo.DepthMapDatas", "KAction_Id");
            CreateIndex("dbo.SkeletonDatas", "KAction_Id");
            CreateIndex("dbo.SingleJoints", "SkeData_Id");
            DropColumn("dbo.ColorFrameDatas", "KinectAction_Id");
            DropColumn("dbo.DepthMapDatas", "KinectAction_Id");
            DropColumn("dbo.SkeletonDatas", "KinectAction_Id");
            DropColumn("dbo.SingleJoints", "SkeletonData_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SingleJoints", "SkeletonData_Id", c => c.Int());
            AddColumn("dbo.SkeletonDatas", "KinectAction_Id", c => c.Int());
            AddColumn("dbo.DepthMapDatas", "KinectAction_Id", c => c.Int());
            AddColumn("dbo.ColorFrameDatas", "KinectAction_Id", c => c.Int());
            DropIndex("dbo.SingleJoints", new[] { "SkeData_Id" });
            DropIndex("dbo.SkeletonDatas", new[] { "KAction_Id" });
            DropIndex("dbo.DepthMapDatas", new[] { "KAction_Id" });
            DropIndex("dbo.ColorFrameDatas", new[] { "KAction_Id" });
            DropForeignKey("dbo.SingleJoints", "SkeData_Id", "dbo.SkeletonDatas");
            DropForeignKey("dbo.SkeletonDatas", "KAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.DepthMapDatas", "KAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.ColorFrameDatas", "KAction_Id", "dbo.KinectActions");
            DropColumn("dbo.SingleJoints", "SkeData_Id");
            DropColumn("dbo.SkeletonDatas", "KAction_Id");
            DropColumn("dbo.DepthMapDatas", "KAction_Id");
            DropColumn("dbo.ColorFrameDatas", "KAction_Id");
            CreateIndex("dbo.SingleJoints", "SkeletonData_Id");
            CreateIndex("dbo.SkeletonDatas", "KinectAction_Id");
            CreateIndex("dbo.DepthMapDatas", "KinectAction_Id");
            CreateIndex("dbo.ColorFrameDatas", "KinectAction_Id");
            AddForeignKey("dbo.SingleJoints", "SkeletonData_Id", "dbo.SkeletonDatas", "Id");
            AddForeignKey("dbo.SkeletonDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.DepthMapDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.ColorFrameDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
        }
    }
}
