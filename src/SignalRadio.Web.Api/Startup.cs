using System.Net.WebSockets;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SignalRadio.Database.EF;
using SignalRadio.Web.Api.Hubs;


namespace SignalRadio.Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureDatabaseServices(services);

            services.AddCors((co) => 
            {
               co.AddPolicy("CorsPolicy", (cpb) => 
               {
                   cpb.AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
               });
            });
            
            services.AddControllers()
                .AddNewtonsoftJson((options) => 
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseCors("CorsPolicy");

            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws/")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var ctx = context.RequestServices.GetService<SignalRadioDbContext>();                      
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var handler = new TrunkRecorderStatusHandler(ctx);

                        await handler.StartStatusMessageHandlerAsync(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers()
                    .RequireCors("CorsPolicy");
                    
                endpoints.MapHub<RadioHub>("/radioHub")
                    .RequireCors("CorsPolicy");
            });
        }
        protected virtual void ConfigureDatabaseServices(IServiceCollection services)
        {   
            services.AddDbContext<SignalRadioDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("SignalRadioDb");
                options.UseSqlite(connectionString, builder => 
                {
                    builder.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                });
            });
        }
    }
}
