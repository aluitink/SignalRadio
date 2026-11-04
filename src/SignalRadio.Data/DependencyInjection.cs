using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SignalRadio.Core.Interfaces;
using SignalRadio.Data.Services;

namespace SignalRadio.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SignalRadioDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
            }));

        services.AddScoped<ITalkGroupsService, TalkGroupsService>();
        services.AddScoped<ICallsService, CallsService>();
        services.AddScoped<IRecordingsService, RecordingsService>();
        services.AddScoped<ITranscriptionsService, TranscriptionsService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ITranscriptSummariesService, TranscriptSummariesService>();

        return services;
    }
}
