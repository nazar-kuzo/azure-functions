using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(AzureFunctions.ModelBinding.FunctionModelBindingStartup))]

namespace AzureFunctions.ModelBinding
{
    internal class FunctionModelBindingStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<FunctionModelBindingExtensionConfigProvider>();
        }
    }
}
