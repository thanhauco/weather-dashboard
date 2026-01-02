using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using TaskMaster.ApiService.Data;
using TaskMaster.ApiService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add PostgreSQL database with EF Core
builder.AddNpgsqlDbContext<TaskManagerDbContext>("tasksdb");

// Add Redis distributed cache
builder.AddRedisClient("cache");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskManagerDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// ============== Weather Endpoints (original) ==============
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithTags("Weather");

// ============== Project Endpoints ==============
var projectsGroup = app.MapGroup("/api/projects").WithTags("Projects");

projectsGroup.MapGet("/", async (TaskManagerDbContext db) =>
{
    return await db.Projects
        .Where(p => !p.IsArchived)
        .Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Color,
            p.CreatedAt,
            TaskCount = p.Tasks.Count,
            CompletedCount = p.Tasks.Count(t => t.Status == TaskItemStatus.Done)
        })
        .ToListAsync();
})
.WithName("GetProjects");

projectsGroup.MapGet("/{id}", async (int id, TaskManagerDbContext db) =>
{
    var project = await db.Projects
        .Include(p => p.Tasks)
        .FirstOrDefaultAsync(p => p.Id == id);
    
    return project is null ? Results.NotFound() : Results.Ok(project);
})
.WithName("GetProject");

projectsGroup.MapPost("/", async (Project project, TaskManagerDbContext db) =>
{
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{project.Id}", project);
})
.WithName("CreateProject");

// ============== Task Endpoints ==============
var tasksGroup = app.MapGroup("/api/tasks").WithTags("Tasks");

tasksGroup.MapGet("/", async (
    TaskManagerDbContext db,
    IConnectionMultiplexer redis,
    TaskItemStatus? status,
    TaskPriority? priority,
    int? projectId) =>
{
    var cacheKey = $"tasks:{status}:{priority}:{projectId}";
    var redisDb = redis.GetDatabase();
    
    // Try cache first
    var cached = await redisDb.StringGetAsync(cacheKey);
    if (cached.HasValue)
    {
        return Results.Ok(JsonSerializer.Deserialize<List<TaskItem>>(cached!));
    }

    // Query database
    var query = db.Tasks.AsQueryable();
    
    if (status.HasValue)
        query = query.Where(t => t.Status == status.Value);
    if (priority.HasValue)
        query = query.Where(t => t.Priority == priority.Value);
    if (projectId.HasValue)
        query = query.Where(t => EF.Property<int?>(t, "ProjectId") == projectId.Value);
    
    var tasks = await query.OrderByDescending(t => t.Priority)
                           .ThenBy(t => t.DueDate)
                           .ToListAsync();

    // Cache for 30 seconds
    await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(tasks), TimeSpan.FromSeconds(30));
    
    return Results.Ok(tasks);
})
.WithName("GetTasks");

tasksGroup.MapGet("/{id}", async (int id, TaskManagerDbContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    return task is null ? Results.NotFound() : Results.Ok(task);
})
.WithName("GetTask");

tasksGroup.MapPost("/", async (TaskItem task, TaskManagerDbContext db, IConnectionMultiplexer redis) =>
{
    task.CreatedAt = DateTime.UtcNow;
    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    
    // Invalidate cache
    var server = redis.GetServer(redis.GetEndPoints().First());
    await foreach (var key in server.KeysAsync(pattern: "tasks:*"))
    {
        await redis.GetDatabase().KeyDeleteAsync(key);
    }
    
    return Results.Created($"/api/tasks/{task.Id}", task);
})
.WithName("CreateTask");

tasksGroup.MapPut("/{id}", async (int id, TaskItem updatedTask, TaskManagerDbContext db, IConnectionMultiplexer redis) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    
    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.Priority = updatedTask.Priority;
    task.Status = updatedTask.Status;
    task.DueDate = updatedTask.DueDate;
    task.AssignedTo = updatedTask.AssignedTo;
    task.Tags = updatedTask.Tags;
    
    if (updatedTask.Status == TaskItemStatus.Done && task.CompletedAt is null)
        task.CompletedAt = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    
    // Invalidate cache
    var server = redis.GetServer(redis.GetEndPoints().First());
    await foreach (var key in server.KeysAsync(pattern: "tasks:*"))
    {
        await redis.GetDatabase().KeyDeleteAsync(key);
    }
    
    return Results.Ok(task);
})
.WithName("UpdateTask");

tasksGroup.MapDelete("/{id}", async (int id, TaskManagerDbContext db, IConnectionMultiplexer redis) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();
    
    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    
    // Invalidate cache
    var server = redis.GetServer(redis.GetEndPoints().First());
    await foreach (var key in server.KeysAsync(pattern: "tasks:*"))
    {
        await redis.GetDatabase().KeyDeleteAsync(key);
    }
    
    return Results.NoContent();
})
.WithName("DeleteTask");

// ============== Dashboard Stats Endpoint ==============
app.MapGet("/api/dashboard/stats", async (TaskManagerDbContext db, IConnectionMultiplexer redis) =>
{
    var cacheKey = "dashboard:stats";
    var redisDb = redis.GetDatabase();
    
    var cached = await redisDb.StringGetAsync(cacheKey);
    if (cached.HasValue)
    {
        return Results.Ok(JsonSerializer.Deserialize<object>(cached!));
    }

    var stats = new
    {
        TotalTasks = await db.Tasks.CountAsync(),
        CompletedTasks = await db.Tasks.CountAsync(t => t.Status == TaskItemStatus.Done),
        InProgressTasks = await db.Tasks.CountAsync(t => t.Status == TaskItemStatus.InProgress),
        OverdueTasks = await db.Tasks.CountAsync(t => t.DueDate < DateTime.UtcNow && t.Status != TaskItemStatus.Done),
        TasksByPriority = await db.Tasks
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(),
        TasksByStatus = await db.Tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(),
        RecentlyCompleted = await db.Tasks
            .Where(t => t.Status == TaskItemStatus.Done)
            .OrderByDescending(t => t.CompletedAt)
            .Take(5)
            .Select(t => new { t.Id, t.Title, t.CompletedAt })
            .ToListAsync(),
        UpcomingDeadlines = await db.Tasks
            .Where(t => t.DueDate != null && t.DueDate > DateTime.UtcNow && t.Status != TaskItemStatus.Done)
            .OrderBy(t => t.DueDate)
            .Take(5)
            .Select(t => new { t.Id, t.Title, t.DueDate, t.Priority })
            .ToListAsync()
    };

    await redisDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(stats), TimeSpan.FromSeconds(10));
    
    return Results.Ok(stats);
})
.WithName("GetDashboardStats")
.WithTags("Dashboard");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
