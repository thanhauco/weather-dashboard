using Microsoft.EntityFrameworkCore;
using TaskMaster.ApiService.Models;

namespace TaskMaster.ApiService.Data;

public class TaskManagerDbContext : DbContext
{
    public TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.AssignedTo).HasMaxLength(100);
            entity.Property(t => t.Tags).HasColumnType("text[]");
            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.Priority);
            entity.HasIndex(t => t.DueDate);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.Color).HasMaxLength(20);
            entity.HasMany(p => p.Tasks)
                  .WithOne()
                  .HasForeignKey("ProjectId")
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial data
        modelBuilder.Entity<Project>().HasData(
            new Project { Id = 1, Name = "Personal", Description = "Personal tasks and reminders", Color = "#10B981" },
            new Project { Id = 2, Name = "Work", Description = "Work-related tasks", Color = "#3B82F6" },
            new Project { Id = 3, Name = "Learning", Description = "Learning and development", Color = "#8B5CF6" }
        );

        modelBuilder.Entity<TaskItem>().HasData(
            new { Id = 1, Title = "Set up development environment", Description = "Install necessary tools and dependencies", Priority = TaskPriority.High, Status = Models.TaskItemStatus.Done, CreatedAt = DateTime.UtcNow.AddDays(-7), CompletedAt = DateTime.UtcNow.AddDays(-5), ProjectId = 2 },
            new { Id = 2, Title = "Learn .NET Aspire", Description = "Complete the official Microsoft Learn path", Priority = TaskPriority.High, Status = Models.TaskItemStatus.InProgress, CreatedAt = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(7), ProjectId = 3 },
            new { Id = 3, Title = "Build demo application", Description = "Create a distributed app with Redis and PostgreSQL", Priority = TaskPriority.Critical, Status = Models.TaskItemStatus.Todo, CreatedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(3), ProjectId = 2 },
            new { Id = 4, Title = "Grocery shopping", Description = "Buy essentials for the week", Priority = TaskPriority.Medium, Status = Models.TaskItemStatus.Todo, CreatedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(1), ProjectId = 1 },
            new { Id = 5, Title = "Read Kubernetes documentation", Priority = TaskPriority.Low, Status = Models.TaskItemStatus.Todo, CreatedAt = DateTime.UtcNow.AddDays(-1), ProjectId = 3 }
        );
    }
}
