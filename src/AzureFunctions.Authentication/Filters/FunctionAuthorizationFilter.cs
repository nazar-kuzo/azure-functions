using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureFunctions.Authentication.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Indexers;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Authentication.Filters
{
    internal class FunctionAuthorizationFilter : IFunctionInvocationFilter
    {
        private readonly AuthorizationMiddleware middleware;

        public FunctionAuthorizationFilter(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationPolicyProvider policyProvider)
        {
            this.middleware = new AuthorizationMiddleware(
                _ =>
                {
                    httpContextAccessor.HttpContext.Items["IsAuthorized"] = true;

                    return Task.CompletedTask;
                },
                policyProvider);
        }

        public async Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            if (executingContext.Arguments.Values.OfType<HttpRequest>().FirstOrDefault()?.HttpContext is HttpContext context)
            {
                var functionDescriptor = context.RequestServices
                    .GetRequiredService<IFunctionIndexLookup>()
                    .LookupByName(executingContext.FunctionName)
                    .Descriptor;

                var authorizeFilters = functionDescriptor
                    .GetPropertyValue<IEnumerable<IFunctionFilter>>("ClassLevelFilters")
                    .Concat(functionDescriptor.GetPropertyValue<IEnumerable<IFunctionFilter>>("MethodLevelFilters"))
                    .ToArray();

                context.SetEndpoint(new Endpoint(
                    requestDelegate: null,
                    new EndpointMetadataCollection(authorizeFilters),
                    displayName: executingContext.FunctionName));

                await this.middleware.Invoke(context);

                if (!context.Items.ContainsKey("IsAuthorized"))
                {
                    await context.Response.CompleteAsync();

                    throw new OperationCanceledException();
                }
            }
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
