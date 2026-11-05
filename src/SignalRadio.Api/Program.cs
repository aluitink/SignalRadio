using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SignalRadio.Api.Agents;
using SignalRadio.Api.Features;
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

// Feature flags + pipeline/summaries tuning
builder.Services.Configure<FeatureFlags>(config.GetSection(FeatureFlags.SectionName));
builder.Services.Configure<PipelineOptions>(config.GetSection(PipelineOptions.SectionName));
builder.Services.Configure<SummariesOptions>(config.GetSection(SummariesOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<FeatureFlags>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<PipelineOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SummariesOptions>>().Value);

// --- Data Access ---
builder.Services.AddDataAccess(connectionString);

// --- Core Services ---
// Scoped: LocalStorageService resolves the base path from the StorageLocation row via the DbContext.
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<ICallNotifier, SignalRCallNotifier>();

// ASR provider: "stub" (default) or "whisper" (openai-whisper-asr-webservice, POST /asr).
var asrProvider = config["ASR:Provider"] ?? "stub";
if (asrProvider == "whisper")
{
    builder.Services.AddHttpClient<IAsrService, WhisperAsrService>(client =>
    {
        // Transcriptions can take a while on CPU; give the client a generous timeout.
        client.Timeout = TimeSpan.FromMinutes(5);
    });
}
else
{
    builder.Services.AddSingleton<IAsrService, StubAsrService>();
}

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

// --- AI agent infrastructure (MAF) ---
builder.Services.AddSingleton<IAgentDefinition, SummariesAgent>();
builder.Services.AddSingleton<AgentRegistry>(sp =>
{
    var registry = new AgentRegistry(
        sp.GetRequiredService<ILogger<AgentRegistry>>(),
        sp.GetRequiredService<IConfiguration>());
    foreach (var def in sp.GetServices<IAgentDefinition>())
        registry.Register(def);
    return registry;
});

builder.Services.AddSingleton<IChatClient>(sp =>
    AgentChatClientFactory.CreateOrStub(
        config["AI:Endpoint"],
        config["AI:ApiKey"],
        config["AI:Model"]));

// --- Background Services ---
if (simulatorEnabled)
{
    builder.Services.AddHostedService<SimulatorService>();
}
builder.Services.AddHostedService<CallProcessingPipeline>();
builder.Services.AddHostedService<SummariesAgentService>();
// NOTE: TranscriptionPipeline (legacy, ungated) was removed — CallProcessingPipeline supersedes it.

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

// Seed data in a fresh scope to avoid any connection state issues from raw SQL above
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<SignalRadioDbContext>();
    var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseInitializer.SeedDataAsync(seedDb, seedLogger);

    // Import rich talk group definitions (name/description/category/priority) from the
    // trunk-recorder CSV. The recorder only yields the talkgroup number on upload, so
    // this ensures the UI has full info for every group. Existing rows are enriched,
    // never overwritten.
    var csvPath = config["TalkGroups:CsvPath"];
    if (!string.IsNullOrWhiteSpace(csvPath))
    {
        try
        {
            await TalkGroupCsvImporter.ImportFromFileAsync(seedDb, csvPath, seedLogger);
        }
        catch (Exception ex)
        {
            seedLogger.LogWarning(ex, "Talk group CSV import failed.");
        }
    }
}

// --- Middleware ---
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TalkGroupHub>("/hubs/talkgroups");

app.Run();
