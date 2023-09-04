using System;
using System.Linq;
using AzureFunctions.Middleware;
using AzureFunctions.Middleware.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FunctionMiddlewareExtensions
    {
        /// <summary>
        /// Registers middleware services that extends default Http Function middleware.
        /// </summary>
        /// <param name="services">Service Collection</param>
        /// <param name="applicationConfigurator">IApplicationBuilder configurator where additional middleware is registered</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddFunctionMiddleware(
            this IServiceCollection services,
            Action<IApplicationBuilder> applicationConfigurator)
        {
            var jobHostMiddlewareServiceType = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .First(x => x.FullName.StartsWith("Microsoft.Azure.WebJobs.Script.WebHost"))
                .GetType("Microsoft.Azure.WebJobs.Script.Middleware.IJobHostHttpMiddleware");

            services.TryAddEnumerable(ServiceDescriptor.Singleton(
                jobHostMiddlewareServiceType,
                ExtendedHttpFunctionMiddleware.CreateTypeProxy(jobHostMiddlewareServiceType)));

            services.AddSingleton(serviceProvider => serviceProvider
                .GetRequiredService<IJobHost>()
                .GetFieldValue<JobHostContext>("_context")
                .FunctionLookup);

            return services.AddSingleton<IApplicationBuilder>(serviceProvider =>
            {
                var applicationBuilder = new ApplicationBuilder(serviceProvider);

                applicationConfigurator.Invoke(applicationBuilder);

                return applicationBuilder;
            });
        }
    }
}
