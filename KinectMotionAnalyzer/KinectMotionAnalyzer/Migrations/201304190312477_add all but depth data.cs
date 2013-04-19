namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addallbutdepthdata : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SkeletonDatas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Status = c.Int(nullable: false),
                        KinectAction_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.KinectActions", t => t.KinectAction_Id)
                .Index(t => t.KinectAction_Id);
            
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SkeletonDatas", t => t.SkeletonData_Id)
                .Index(t => t.SkeletonData_Id);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.SingleJoints", new[] { "SkeletonData_Id" });
            DropIndex("dbo.SkeletonDatas", new[] { "KinectAction_Id" });
            DropForeignKey("dbo.SingleJoints", "SkeletonData_Id", "dbo.SkeletonDatas");
            DropForeignKey("dbo.SkeletonDatas", "KinectAction_Id", "dbo.KinectActions");
            DropTable("dbo.SingleJoints");
            DropTable("dbo.SkeletonDatas");
        }
    }
}
