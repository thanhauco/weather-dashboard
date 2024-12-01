# 📋 Task Manager - .NET Aspire Distributed Application

A modern task management application built with **.NET Aspire** demonstrating cloud-native distributed application development with PostgreSQL, Redis, and a Blazor Server frontend.

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    .NET Aspire AppHost                       │
│  (Orchestrates all services and infrastructure)             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐     │
│  │   Blazor    │───▶│  API        │───▶│ PostgreSQL  │     │
│  │   Web App   │    │  Service    │    │  Database   │     │
│  └─────────────┘    └──────┬──────┘    └─────────────┘     │
│                            │                                │
│                            ▼                                │
│                    ┌─────────────┐                         │
│                    │   Redis     │                         │
│                    │   Cache     │                         │
│                    └─────────────┘                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## ✨ Features

### Dashboard

- 📊 Real-time task statistics (total, completed, in-progress, overdue)
- 📅 Upcoming deadlines view
- 🏆 Recently completed tasks
- 📈 Tasks by status chart

### Tasks Management

- ✅ Full CRUD operations (Create, Read, Update, Delete)
- 🎨 Priority levels (Low, Medium, High, Critical)
- 📋 Status tracking (Todo, In Progress, Review, Done)
- 🔍 Filtering by status and priority
- 📱 Responsive grid layout

### Projects

- 📁 Project organization with color coding
- 📊 Progress tracking with visual indicators
- 📋 Task count per project

### Infrastructure

- 🐘 **PostgreSQL** - Persistent data storage with Entity Framework Core
- ⚡ **Redis** - Distributed caching for API responses
- 📊 **Aspire Dashboard** - Built-in observability and monitoring
- 🔧 **pgAdmin** - Database management UI
- 🔍 **Redis Insight** - Cache inspection UI

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for containers)
- .NET Aspire workload:
  ```bash
  dotnet workload install aspire
  ```

### Running the Application

1. **Clone and navigate to the project**

   ```bash
   cd ruby-andromeda
   ```

2. **Run the Aspire AppHost**

   ```bash
   dotnet run --project WeatherDashboard.AppHost
   ```

3. **Open the Aspire Dashboard**

   - Navigate to the URL shown in the console (typically `https://localhost:17XXX`)
   - View all running services, logs, and traces

4. **Access the Web Application**
   - Click on the `webfrontend` endpoint in the Aspire Dashboard
   - Or navigate directly to the URL shown

## 📁 Project Structure

```
ruby-andromeda/
├── WeatherDashboard.AppHost/          # Aspire orchestration
│   └── Program.cs                     # Service composition
├── WeatherDashboard.ApiService/       # REST API
│   ├── Program.cs                     # API endpoints
│   ├── Data/
│   │   └── TaskManagerDbContext.cs    # EF Core DbContext
│   └── Models/
│       ├── TaskItem.cs                # Task entity
│       └── Project.cs                 # Project entity
├── WeatherDashboard.Web/              # Blazor Server frontend
│   ├── TaskManagerApiClient.cs        # Typed HTTP client
│   └── Components/
│       └── Pages/
│           ├── Home.razor             # Dashboard
│           ├── Tasks.razor            # Task management
│           ├── Projects.razor         # Project view
│           └── Weather.razor          # Weather demo
└── WeatherDashboard.ServiceDefaults/  # Shared configuration
```

## 🔌 API Endpoints

### Tasks

| Method | Endpoint          | Description                        |
| ------ | ----------------- | ---------------------------------- |
| GET    | `/api/tasks`      | List tasks (with optional filters) |
| GET    | `/api/tasks/{id}` | Get task by ID                     |
| POST   | `/api/tasks`      | Create a new task                  |
| PUT    | `/api/tasks/{id}` | Update a task                      |
| DELETE | `/api/tasks/{id}` | Delete a task                      |

### Projects

| Method | Endpoint             | Description            |
| ------ | -------------------- | ---------------------- |
| GET    | `/api/projects`      | List all projects      |
| GET    | `/api/projects/{id}` | Get project with tasks |
| POST   | `/api/projects`      | Create a new project   |

### Dashboard

| Method | Endpoint               | Description              |
| ------ | ---------------------- | ------------------------ |
| GET    | `/api/dashboard/stats` | Get dashboard statistics |

### Weather (Demo)

| Method | Endpoint           | Description          |
| ------ | ------------------ | -------------------- |
| GET    | `/weatherforecast` | Get weather forecast |

## 🛠️ Technology Stack

- **Runtime**: .NET 8
- **Orchestration**: .NET Aspire 8.2
- **Frontend**: Blazor Server
- **API**: ASP.NET Core Minimal APIs
- **Database**: PostgreSQL with Entity Framework Core
- **Caching**: Redis (StackExchange.Redis)
- **Containers**: Docker

## 📚 Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor)
- [Entity Framework Core](https://learn.microsoft.com/ef/core)

## 📝 License

This project is for educational purposes demonstrating .NET Aspire capabilities.
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
 
