
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;
using Microsoft.EntityFrameworkCore;
namespace OCPP.Core.Server
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            try
            {
                // Force the EF model creation for faster startup
                // Construir DbContextOptions usando IConfiguration
                var optionsBuilder = new DbContextOptionsBuilder<OCPPCoreContext>();
                optionsBuilder.UseSqlServer(config.GetConnectionString("SqlServer"));

                // Crear una instancia de OCPPCoreContext usando DbContextOptions
                using (var dbContext = new OCPPCoreContext(optionsBuilder.Options))
                {
                    IModel model = dbContext.Model;
                }

                CreateHostBuilder(args).Build().Run();
            }
            catch //(Exception e)
            {
                //logger.Error(e, "OCPP server stopped because of exception");
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .ConfigureLogging((ctx, builder) =>
                                        {
                                            builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                                            builder.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                                        })
                    .UseStartup<Startup>();
                });
    }
}
