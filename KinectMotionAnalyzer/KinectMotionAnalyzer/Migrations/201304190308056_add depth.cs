namespace KinectMotionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class adddepth : DbMigration
    {
        public override void Up()
        {
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.KinectActions", t => t.KinectAction_Id)
                .Index(t => t.KinectAction_Id);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.DepthMapDatas", new[] { "KinectAction_Id" });
            DropForeignKey("dbo.DepthMapDatas", "KinectAction_Id", "dbo.KinectActions");
            DropTable("dbo.DepthMapDatas");
        }
    }
}
