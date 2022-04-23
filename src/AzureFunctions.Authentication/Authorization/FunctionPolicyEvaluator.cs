using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Azure.Functions.Authentication.Authorization
{
    /// <summary>
    /// Overrides default policy evaluator to pass combined default and application authorization handlers.
    /// See <see cref="FunctionAuthorizationHandlerProvider"/> for more details
    /// </summary>
    internal class FunctionPolicyEvaluator : PolicyEvaluator
    {
        public FunctionPolicyEvaluator(
            IAuthorizationPolicyProvider policyProvider,
            IAuthorizationHandlerProvider defaultHandlerProvider,
            IEnumerable<IAuthorizationHandler> functionHandlers,
            ILogger<DefaultAuthorizationService> logger,
            IAuthorizationHandlerContextFactory contextFactory,
            IAuthorizationEvaluator evaluator)
            : base(new DefaultAuthorizationService(
                policyProvider,
                new FunctionAuthorizationHandlerProvider(defaultHandlerProvider, functionHandlers),
                logger,
                contextFactory,
                evaluator,
                GetAuthorizationOptions(policyProvider)))
        {
        }

        private static IOptions<AuthorizationOptions> AuthorizationOptions { get; set; }

        private static IOptions<AuthorizationOptions> GetAuthorizationOptions(IAuthorizationPolicyProvider policyProvider)
        {
            if (AuthorizationOptions == null)
            {
                AuthorizationOptions = Options.Create(policyProvider.GetAuthorizationOptions());
            }

            return AuthorizationOptions;
        }
    }
}
