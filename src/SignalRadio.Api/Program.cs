using SignalRadio.Api.Hubs;
using SignalRadio.Api.Services;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data;
using SignalRadio.Simulator;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var config = builder.Configuration;
var connectionString = config.GetConnectionString("Default") ?? "Host=localhost;Port=5432;Database=signalradio;Username=signalradio;Password=signalradio";
var aiEnabled = config.GetValue<bool>("AI:Enabled");
var simulatorEnabled = config.GetValue<bool>("Simulator:Enabled");

// --- Data Access ---
builder.Services.AddDataAccess(connectionString);

// --- Core Services ---
// Scoped: LocalStorageService resolves the base path from the StorageLocation row via the DbContext.
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<ICallNotifier, SignalRCallNotifier>();
builder.Services.AddSingleton<IAsrService, StubAsrService>();

// AI client: stub / OpenAI-compatible / local ONNX
var embeddingProvider = config["AI:EmbeddingProvider"] ?? "none"; // none | openai | local
if (aiEnabled)
{
    builder.Services.AddSingleton<IAiClient, OpenAiCompatibleClient>();
}
else if (embeddingProvider == "local")
{
    builder.Services.AddSingleton<IAiClient, LocalEmbeddingClient>();
}
else
{
    builder.Services.AddSingleton<IAiClient, StubAiClient>();
}

// --- SignalR ---
builder.Services.AddSignalR();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// --- Controllers ---
builder.Services.AddControllers();

// --- Background Services ---
if (simulatorEnabled)
{
    builder.Services.AddHostedService<SimulatorService>();
}
builder.Services.AddHostedService<TranscriptionPipeline>();

var app = builder.Build();

// --- Database Initialization ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Wait for database to be ready
    var retries = 30;
    var connected = false;
    while (retries-- > 0 && !connected)
    {
        try
        {
            await db.Database.CanConnectAsync();
            connected = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database not ready, retrying in 2s... ({Retries} left)", retries);
            await Task.Delay(2000);
        }
    }

    if (!connected)
    {
        logger.LogError("Could not connect to database after {Retries} retries. Exiting.", retries);
        Environment.Exit(1);
    }

    await DatabaseInitializer.InitializeAsync(db, logger);
}

// --- Middleware ---
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TalkGroupHub>("/hubs/talkgroups");

app.Run();
