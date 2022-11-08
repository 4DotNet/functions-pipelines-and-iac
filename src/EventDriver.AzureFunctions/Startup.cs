using Microsoft.Extensions.Configuration;
using System;
using Azure.Identity;
using EventDriver.AzureFunctions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace EventDriver.AzureFunctions
{
    public class Startup : FunctionsStartup
    {

        private IConfigurationRoot _configuration;
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var azureCredential = new DefaultAzureCredential();

            try
            {
                var azureAppConfigurationEndpoint = Environment.GetEnvironmentVariable("AzureAppConfiguration") ?? "";
                builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(new Uri(azureAppConfigurationEndpoint), azureCredential)
                        .ConfigureKeyVault(kv => kv.SetCredential(azureCredential))
                        .UseFeatureFlags();
                });
                _configuration = builder.ConfigurationBuilder.Build();
            }
            catch (Exception ex)
            {
                throw new Exception("Configuration failed", ex);
            }

            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            //builder.Services.AddHexMasterCache(_configuration);
        }
    }
}
