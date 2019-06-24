# MiniProfiler Initialization for SQL Server, SQLite and SQLite InMemory
[![nuget](https://img.shields.io/nuget/v/MiniProfilerDb.Initialization.svg)](https://www.nuget.org/packages/MiniProfilerDb.Initialization/) ![Downloads](https://img.shields.io/nuget/dt/MiniProfilerDb.Initialization.svg "Downloads")

* [MiniProfiler ASP.NET Core](https://miniprofiler.com/dotnet/AspDotNetCore)
* I have created the following extension methods which aims to allow MiniProfiler to be initialized & destroyed independently in a similar manner to how EF Core initialization works. A key addition is the ability to create a new database if it doesn't already exist.
* Supports SQL Server, SQLite and SQLite InMemory.

## Installation

### NuGet
```
PM> Install-Package MiniProfilerDb.Initialization
```

### .Net CLI
```
> dotnet add package MiniProfilerDb.Initialization
```

## Nuget packages
* MiniProfiler.AspNetCore.Mvc
* MiniProfiler.EntityFrameworkCore
* MiniProfiler.Providers.SqlServer
* MiniProfiler.Providers.Sqlite

## Usage
* await MiniProfilerInitializer.EnsureTablesDeletedAsync(connectionString) = Ensures only tables related to MiniProfiler are deleted.
* await MiniProfilerInitializer.EnsureDbCreatedAsync(connectionString) = Ensures MiniProfiler physical database is created in preparation for MiniProfiler to create schema.
* await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(connectionString) = Ensures MiniProfiler physical database is created and tables created.
* await MiniProfilerInitializer.EnsureDbDestroyedAsync(connectionString) = Deletes physical database.
* Ensure Language Version is set to 'C# latest minor version (latest) to allow async Main.
* Ensure Main method is async.
```
 public class Program
{
	public static async Task Main(string[] args)
	{
		
	}
}
```

## Development and Integration Environment Example
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=MiniProfilerDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
await MiniProfilerInitializer.EnsureTablesDeleted(sqlServerConnectionString);
await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(sqlServerConnectionString);

var sqliteConnectionString = "Data Source=MiniProfiler.db;";
await MiniProfilerInitializer.EnsureTablesDeletedAsync(sqliteConnectionString);
await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(sqliteConnectionString);
```

## Staging and Production Environment Example
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=MiniProfilerDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(sqlServerConnectionString);

var sqliteConnectionString = "Data Source=MiniProfiler.db;";
await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(sqliteConnectionString);
```

## Example
```
public static async Task Main(string[] args)
{
     var host = CreateWebHostBuilder(args).Build();

    using (var scope = host.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var configuration = services.GetRequiredService<IConfiguration>();
			var connectionString = configuration.GetConnectionString("DefaultConnection");
            await MiniProfilerInitializer.EnsureTablesDeletedAsync(connectionString);
			await MiniProfilerInitializer.EnsureDbAndTablesCreatedAsync(connectionString);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing MiniProfiler db.");
        }
    }

    host.Run();
}

public class Startup
{
     public IConfiguration Configuration { get; }

	 public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}
	
	public virtual void ConfigureServices(IServiceCollection services)
	{
		  services.AddMiniProfiler(options => {
                options.PopupRenderPosition = StackExchange.Profiling.RenderPosition.BottomLeft;
                options.PopupStartHidden = true; //ALT + P to display
                options.PopupShowTimeWithChildren = true;
                options.ResultsAuthorize = (request) => true;
                options.UserIdProvider = (request) => request.HttpContext.User.Identity.Name;
                //options.Storage = new SqlServerStorage();
            }).AddEntityFramework();
	}

	public void Configure(IApplicationBuilder app)
	{
		// ...existing configuration...
		app.UseMiniProfiler();

		// The call to app.UseMiniProfiler must come before the call to app.UseMvc
		app.UseMvc(routes =>
		{
			// ...
		});
			
	}
}
```

## See Also
* [Database Initialization](https://github.com/davidikin45/Database.Initialization)
* [Hangfire Initialization](https://github.com/davidikin45/Hangfire.Initialization)
* [EntityFrameworkCore Initialization](https://github.com/davidikin45/EntityFrameworkCore.Initialization)