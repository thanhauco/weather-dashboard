using System.Net.Http.Json;

namespace WeatherDashboard.Web;

public class TaskManagerApiClient(HttpClient httpClient)
{
    // ============== Weather ==============
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }

    // ============== Tasks ==============
    public async Task<List<TaskItem>> GetTasksAsync(TaskItemStatus? status = null, TaskPriority? priority = null, int? projectId = null)
    {
        var query = new List<string>();
        if (status.HasValue) query.Add($"status={status}");
        if (priority.HasValue) query.Add($"priority={priority}");
        if (projectId.HasValue) query.Add($"projectId={projectId}");

        var url = "/api/tasks" + (query.Any() ? "?" + string.Join("&", query) : "");
        return await httpClient.GetFromJsonAsync<List<TaskItem>>(url) ?? [];
    }

    public async Task<TaskItem?> GetTaskAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<TaskItem>($"/api/tasks/{id}");
    }

    public async Task<TaskItem> CreateTaskAsync(TaskItem task)
    {
        var response = await httpClient.PostAsJsonAsync("/api/tasks", task);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskItem>() ?? task;
    }

    public async Task<TaskItem> UpdateTaskAsync(int id, TaskItem task)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/tasks/{id}", task);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskItem>() ?? task;
    }

    public async Task DeleteTaskAsync(int id)
    {
        var response = await httpClient.DeleteAsync($"/api/tasks/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ============== Projects ==============
    public async Task<List<ProjectSummary>> GetProjectsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<ProjectSummary>>("/api/projects") ?? [];
    }

    public async Task<Project?> GetProjectAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<Project>($"/api/projects/{id}");
    }

    // ============== Dashboard ==============
    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        return await httpClient.GetFromJsonAsync<DashboardStats>("/api/dashboard/stats");
    }
}

// ============== Models ==============
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AssignedTo { get; set; }
    public List<string> Tags { get; set; } = [];
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3,
    Archived = 4
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }
    public List<TaskItem> Tasks { get; set; } = [];
}

public class ProjectSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#3B82F6";
    public DateTime CreatedAt { get; set; }
    public int TaskCount { get; set; }
    public int CompletedCount { get; set; }
}

public class DashboardStats
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public List<PriorityCount> TasksByPriority { get; set; } = [];
    public List<StatusCount> TasksByStatus { get; set; } = [];
    public List<RecentTask> RecentlyCompleted { get; set; } = [];
    public List<UpcomingTask> UpcomingDeadlines { get; set; } = [];
}

public class PriorityCount
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class StatusCount
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
}

public class UpcomingTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; }
}
