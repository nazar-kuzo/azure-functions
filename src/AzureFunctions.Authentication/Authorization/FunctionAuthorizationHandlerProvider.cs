using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Azure.Functions.Authentication.Authorization
{
    /// <summary>
    /// Combines default Azure Function authorization handler with custom application authorization handler
    /// </summary>
    internal class FunctionAuthorizationHandlerProvider : IAuthorizationHandlerProvider
    {
        private readonly IAuthorizationHandlerProvider defaultHandlerProvider;
        private readonly IEnumerable<IAuthorizationHandler> functionHandlers;

        public FunctionAuthorizationHandlerProvider(
            IAuthorizationHandlerProvider defaultHandlerProvider,
            IEnumerable<IAuthorizationHandler> functionHandlers)
        {
            this.defaultHandlerProvider = defaultHandlerProvider;
            this.functionHandlers = functionHandlers;
        }

        public async Task<IEnumerable<IAuthorizationHandler>> GetHandlersAsync(AuthorizationHandlerContext context)
        {
            return (await this.defaultHandlerProvider.GetHandlersAsync(context)).Union(this.functionHandlers);
        }
    }
}
