using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SignalRadio.DataAccess.Services;

namespace SignalRadio.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SignalRadioDbContext>(options =>
            options.UseSqlServer(connectionString));

    // Register data-access services (service layer extracted from Controller2)
    services.AddScoped<IRecordingsService, RecordingsService>();
    services.AddScoped<ICallsService, CallsService>();
    services.AddScoped<IStorageLocationsService, StorageLocationsService>();
    services.AddScoped<ITalkGroupsService, TalkGroupsService>();
    services.AddScoped<ITranscriptionsService, TranscriptionsService>();

        return services;
    }
}
