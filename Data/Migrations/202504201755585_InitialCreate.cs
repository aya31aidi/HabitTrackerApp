namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Habits",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        CategoryId = c.Int(nullable: false),
                        Goal = c.String(maxLength: 255),
                        UserId = c.Int(nullable: false),
                        HabitStatistic_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categories", t => t.CategoryId, cascadeDelete: true)
                .ForeignKey("dbo.HabitStatistics", t => t.HabitStatistic_Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.CategoryId)
                .Index(t => t.UserId)
                .Index(t => t.HabitStatistic_Id);
            
            CreateTable(
                "dbo.HabitRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        IsCompleted = c.Boolean(nullable: false),
                        HabitId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Habits", t => t.HabitId, cascadeDelete: true)
                .Index(t => t.HabitId);
            
            CreateTable(
                "dbo.HabitStatistics",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TotalCompletions = c.Int(nullable: false),
                        Streak = c.Int(nullable: false),
                        CompletionRate = c.Double(nullable: false),
                        HabitId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false),
                        Password = c.String(nullable: false, maxLength: 50),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Reminders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Message = c.String(nullable: false, maxLength: 200),
                        ReminderTime = c.DateTime(nullable: false),
                        HabitId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Habits", t => t.HabitId, cascadeDelete: true)
                .Index(t => t.HabitId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Reminders", "HabitId", "dbo.Habits");
            DropForeignKey("dbo.Habits", "UserId", "dbo.Users");
            DropForeignKey("dbo.Habits", "HabitStatistic_Id", "dbo.HabitStatistics");
            DropForeignKey("dbo.HabitRecords", "HabitId", "dbo.Habits");
            DropForeignKey("dbo.Habits", "CategoryId", "dbo.Categories");
            DropIndex("dbo.Reminders", new[] { "HabitId" });
            DropIndex("dbo.HabitRecords", new[] { "HabitId" });
            DropIndex("dbo.Habits", new[] { "HabitStatistic_Id" });
            DropIndex("dbo.Habits", new[] { "UserId" });
            DropIndex("dbo.Habits", new[] { "CategoryId" });
            DropTable("dbo.Reminders");
            DropTable("dbo.Users");
            DropTable("dbo.HabitStatistics");
            DropTable("dbo.HabitRecords");
            DropTable("dbo.Habits");
            DropTable("dbo.Categories");
        }
    }
}
