using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(Azure.Functions.Authentication.FunctionAuthenticationStartup))]

namespace Azure.Functions.Authentication
{
    internal class FunctionAuthenticationStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<FunctionAuthenticationExtensionConfigProvider>();
        }
    }
}
