using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureFunctions.Authentication.Authorization
{
    /// <summary>
    /// Overrides default policy evaluator to pass combined default and application authorization handlers.
    /// See <see cref="FunctionAuthorizationHandlerProvider"/> for more details
    /// </summary>
    internal class FunctionPolicyEvaluator : PolicyEvaluator
    {
        private readonly IAuthorizationPolicyProvider policyProvider;

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
            this.policyProvider = policyProvider;
        }

        public override async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
        {
            if (policy.AuthenticationSchemes.Count == 0)
            {
                policy = new AuthorizationPolicy(policy.Requirements, (await this.policyProvider.GetDefaultPolicyAsync()).AuthenticationSchemes);
            }

            return await base.AuthenticateAsync(policy, context);
        }

        private static IOptions<AuthorizationOptions> AuthorizationOptions { get; set; }

        private static IOptions<AuthorizationOptions> GetAuthorizationOptions(IAuthorizationPolicyProvider policyProvider)
        {
            return AuthorizationOptions ??= Options.Create(policyProvider.GetAuthorizationOptions());
        }
    }
}
