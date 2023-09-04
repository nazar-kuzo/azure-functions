using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(AzureFunctions.Authentication.FunctionAuthenticationStartup))]

namespace AzureFunctions.Authentication
{
    internal class FunctionAuthenticationStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<FunctionAuthenticationExtensionConfigProvider>();
        }
    }
}
