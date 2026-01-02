var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres");

var tasksDb = postgres.AddDatabase("tasksdb");

// Add API service with dependencies
var apiService = builder.AddProject<Projects.TaskMaster_ApiService>("apiservice")
    .WithReference(cache)
    .WithReference(tasksDb);

// Add Web frontend
builder.AddProject<Projects.TaskMaster_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

builder.Build().Run();
