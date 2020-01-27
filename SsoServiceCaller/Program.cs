using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SsoServiceCaller
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static async Task Main(string[] args)
        {
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";
            //Determines the working environment as IHostingEnvironment is unavailable in a console app

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            if (isDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets<Secrets>()
                       .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            }

            Configuration = builder.Build();

            var services = new ServiceCollection()
                .Configure<Secrets>(Configuration.GetSection(nameof(Secrets)))
                .Configure<ConfigSettings>(Configuration.GetSection(nameof(ConfigSettings)))
                .AddOptions()
                .AddTransient<ApiCaller, ApiCaller>()
                .BuildServiceProvider();

            var apiCaller = services.GetService<ApiCaller>();
            await apiCaller.CallApiAsync();
        }
    }
}
