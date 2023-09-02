using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;

namespace AzureFunctions.Authentication.Filters
{
    internal class FunctionAuthenticationFilter : IFunctionInvocationFilter
    {
        private readonly AuthenticationMiddleware middleware;

        public FunctionAuthenticationFilter(IAuthenticationSchemeProvider schemes)
        {
            this.middleware = new AuthenticationMiddleware(_ => Task.CompletedTask, schemes);
        }

        public async Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            if (executingContext.Arguments.Values.OfType<HttpRequest>().FirstOrDefault()?.HttpContext is HttpContext context)
            {
                await this.middleware.Invoke(context);
            }
        }

        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
