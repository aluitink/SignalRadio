using SignalRadio.Core.Models;
using SignalRadio.Core.Services;
using SignalRadio.Core.Data;
using SignalRadio.Core.Repositories;
using SignalRadio.Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;
using DotNetEnv;

// Load .env file if it exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for standalone UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Entity Framework
builder.Services.AddDbContext<SignalRadioDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("SignalRadio.Api"));
});

// Configure Azure Storage
builder.Services.Configure<AzureStorageOptions>(
    builder.Configuration.GetSection(AzureStorageOptions.Section));

// Configure Local Storage
builder.Services.Configure<LocalStorageOptions>(
    builder.Configuration.GetSection("LocalStorage"));

// Configure ASR Settings
builder.Services.Configure<AsrOptions>(
    builder.Configuration.GetSection(AsrOptions.SectionName));

// Register repositories
builder.Services.AddScoped<ICallRepository, CallRepository>();
builder.Services.AddScoped<IRecordingRepository, RecordingRepository>();
builder.Services.AddScoped<ITalkGroupRepository, TalkGroupRepository>();

// Register services
// Choose storage service based on config
var storageType = builder.Configuration["StorageType"] ?? "Azure";
if (storageType.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IStorageService, LocalDiskStorageService>();
}
else
{
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
}
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<ITalkGroupService, TalkGroupService>();
builder.Services.AddScoped<ISearchService, FullTextSearchService>();

// Register ASR services
builder.Services.AddHttpClient<WhisperAsrService>();
builder.Services.AddScoped<IAsrService, WhisperAsrService>();

// Register background services
// Configure LocalFileCacheService options
builder.Services.Configure<SignalRadio.Api.Services.LocalFileCacheOptions>(
    builder.Configuration.GetSection("LocalFileCache"));
builder.Services.AddSingleton<SignalRadio.Api.Services.ILocalFileCacheService, SignalRadio.Api.Services.LocalFileCacheService>();

// Register background services
builder.Services.AddHostedService<SignalRadio.Api.Services.TranscriptionBackgroundService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Run database migrations on startup, but wait for SQL Server to be ready first.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
    var logger = app.Logger;

    // Allow configuring how long to wait for the DB to become ready (seconds)
    var timeoutSeconds = app.Configuration.GetValue<int>("DbStartupTimeoutSeconds", 60);
    var maxWait = TimeSpan.FromSeconds(timeoutSeconds);
    var delay = TimeSpan.FromSeconds(1);
    var sw = Stopwatch.StartNew();

    // Poll until the DB is reachable or we've timed out.
    while (true)
    {
        try
        {
            if (await context.Database.CanConnectAsync())
            {
                logger.LogInformation("Database is reachable; continuing startup.");
                break;
            }
        }
        catch (SqlException ex)
        {
            // SQL Server may not be ready; log and retry.
            logger.LogInformation(ex, "Database not ready yet (SqlException), retrying...");
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Unexpected error while checking DB readiness, retrying...");
        }

        if (sw.Elapsed > maxWait)
        {
            logger.LogError("Timed out waiting for database to become ready after {Timeout}s", timeoutSeconds);
            break;
        }

        logger.LogInformation("Waiting {Delay}s for database to become ready...", delay.TotalSeconds);
        await Task.Delay(delay);
        // Exponential backoff up to 5s
        delay = TimeSpan.FromSeconds(Math.Min(5, delay.TotalSeconds * 2));
    }

    try
    {
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");
    }
    catch (SqlException ex) when (ex.Number == 1801)
    {
        // SQL Server error 1801 = Database already exists. Race between processes creating DB.
        logger.LogWarning(ex, "Database already exists (race). Continuing without failing startup.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowUI");

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<TalkGroupHub>("/hubs/talkgroup");

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
