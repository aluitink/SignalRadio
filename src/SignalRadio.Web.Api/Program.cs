using Microsoft.EntityFrameworkCore;

using SignalRadio.Database.EF;

namespace SignalRadio.Web.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole();
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

            // Add services to the container.
            builder.Configuration.AddJsonFile("appsettings.json");               

            builder.Services.AddDbContext<SignalRadioDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("SignalRadioDb");
                options.EnableDetailedErrors(true);
                options.UseSqlServer(connectionString, (opts) => opts.EnableRetryOnFailure());
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseWebSockets();

            app.Run();
        }
    }
}