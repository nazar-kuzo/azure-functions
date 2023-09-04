using System;
using AzureFunctions.Authentication.Extensions;
using AzureFunctions.Authentication.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureFunctions.Authentication
{
    /// <summary>
    /// Extension that registers custom authentication scheme after application finished bootstrap.
    /// This step is required to avoid overriding internal authentication schemes registration
    /// </summary>
    internal class FunctionAuthenticationExtensionConfigProvider : IExtensionConfigProvider
    {
        private static bool initialized = false;

        private readonly AuthenticationOptions authenticationOptions;
        private readonly ILogger<FunctionAuthenticationExtensionConfigProvider> logger;
        private readonly OptionsConfigurator<AuthorizationOptions> authorizationOptionsConfigurator;
        private readonly IAuthenticationSchemeProvider schemeProvider;
        private readonly IAuthorizationPolicyProvider policyProvider;

        public FunctionAuthenticationExtensionConfigProvider(
            ILogger<FunctionAuthenticationExtensionConfigProvider> logger,
            IOptions<AuthenticationOptions> authenticationOptions,
            OptionsConfigurator<AuthorizationOptions> authorizationOptionsConfigurator,
            IAuthenticationSchemeProvider schemeProvider,
            IAuthorizationPolicyProvider policyProvider)
        {
            this.logger = logger;
            this.authenticationOptions = authenticationOptions.Value;
            this.authorizationOptionsConfigurator = authorizationOptionsConfigurator;
            this.schemeProvider = schemeProvider;
            this.policyProvider = policyProvider;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            var authenticationOptions = this.schemeProvider.GetFieldValue<AuthenticationOptions>("_options");

            authenticationOptions.DefaultAuthenticateScheme = this.authenticationOptions.DefaultAuthenticateScheme;
            authenticationOptions.DefaultChallengeScheme = this.authenticationOptions.DefaultChallengeScheme;
            authenticationOptions.DefaultForbidScheme = this.authenticationOptions.DefaultForbidScheme;
            authenticationOptions.DefaultScheme = this.authenticationOptions.DefaultScheme;
            authenticationOptions.DefaultSignInScheme = this.authenticationOptions.DefaultSignInScheme;
            authenticationOptions.DefaultSignOutScheme = this.authenticationOptions.DefaultSignOutScheme;
            authenticationOptions.RequireAuthenticatedSignIn = this.authenticationOptions.RequireAuthenticatedSignIn;

            this.authorizationOptionsConfigurator
                ?.Configure(this.policyProvider.GetAuthorizationOptions());

            foreach (var newScheme in this.authenticationOptions.Schemes)
            {
                try
                {
                    authenticationOptions.AddScheme(newScheme.Name, scheme =>
                    {
                        scheme.DisplayName = newScheme.DisplayName;
                        scheme.HandlerType = newScheme.HandlerType;
                    });

                    this.schemeProvider.AddScheme(newScheme.Build());
                }
                catch (InvalidOperationException ex)
                when (ex.Message.StartsWith("Scheme already exists:"))
                {
                    if (newScheme.Name == "Bearer")
                    {
                        this.logger.LogError("\"Bearer\" scheme name is preserved, please use the other one: \"CustomBearer\", \"B2B\", etc.");
                    }
                    else
                    {
                        this.logger.LogError(ex.Message);
                    }
                }
            }
        }
    }
}
