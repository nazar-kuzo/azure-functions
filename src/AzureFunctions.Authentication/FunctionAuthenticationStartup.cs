using AzureFunctions.Authentication.Extensions;
using AzureFunctions.Authentication.Filters;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Azure.Functions.Authentication.FunctionAuthenticationStartup))]

namespace Azure.Functions.Authentication
{
    internal class FunctionAuthenticationStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddSingleton(serviceProvider =>
            {
                return serviceProvider
                    .GetRequiredService<IJobHost>()
                    .GetFieldValue<JobHostContext>("_context")
                    .FunctionLookup;
            });

            builder.Services.AddSingleton<IFunctionFilter, FunctionAuthenticationFilter>();
            builder.Services.AddSingleton<IFunctionFilter, FunctionAuthorizationFilter>();

            builder.AddExtension<FunctionAuthenticationExtensionConfigProvider>();
        }
    }
}
