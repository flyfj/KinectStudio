namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class keeponlycolor : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.KinectActions", "TestColor_Id", "dbo.ColorFrameDatas");
            DropForeignKey("dbo.DepthMapDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.SkeletonDatas", "KinectAction_Id", "dbo.KinectActions");
            DropForeignKey("dbo.SingleJoints", "SkeletonData_Id", "dbo.SkeletonDatas");
            DropIndex("dbo.KinectActions", new[] { "TestColor_Id" });
            DropIndex("dbo.DepthMapDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.SkeletonDatas", new[] { "KinectAction_Id" });
            DropIndex("dbo.SingleJoints", new[] { "SkeletonData_Id" });
            DropColumn("dbo.KinectActions", "TestColor_Id");
            DropTable("dbo.DepthMapDatas");
            DropTable("dbo.SkeletonDatas");
            DropTable("dbo.SingleJoints");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.SingleJoints",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Type = c.Int(nullable: false),
                        PosX = c.Single(nullable: false),
                        PosY = c.Single(nullable: false),
                        PosZ = c.Single(nullable: false),
                        SkeletonData_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SkeletonDatas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Status = c.Int(nullable: false),
                        KinectAction_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DepthMapDatas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FrameId = c.Int(nullable: false),
                        FrameWidth = c.Int(nullable: false),
                        FrameHeight = c.Int(nullable: false),
                        KinectAction_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.KinectActions", "TestColor_Id", c => c.Int());
            CreateIndex("dbo.SingleJoints", "SkeletonData_Id");
            CreateIndex("dbo.SkeletonDatas", "KinectAction_Id");
            CreateIndex("dbo.DepthMapDatas", "KinectAction_Id");
            CreateIndex("dbo.KinectActions", "TestColor_Id");
            AddForeignKey("dbo.SingleJoints", "SkeletonData_Id", "dbo.SkeletonDatas", "Id");
            AddForeignKey("dbo.SkeletonDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.DepthMapDatas", "KinectAction_Id", "dbo.KinectActions", "Id");
            AddForeignKey("dbo.KinectActions", "TestColor_Id", "dbo.ColorFrameDatas", "Id");
        }
    }
}
