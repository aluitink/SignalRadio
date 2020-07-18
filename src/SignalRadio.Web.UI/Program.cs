using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SignalRadio.Web.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

       public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .ConfigureKestrel(serverOptions => {
                        serverOptions
                        .ConfigureHttpsDefaults(listenOptions => {
                            listenOptions.ServerCertificate = new X509Certificate2("../.certs/localhost.pfx", new NetworkCredential("", "Davenport252").SecurePassword);
                        });
                    })
                    .UseStartup<Startup>();
                });
    }
}
