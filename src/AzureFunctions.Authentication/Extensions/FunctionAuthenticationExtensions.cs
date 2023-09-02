using System;
using Azure.Functions.Authentication.Authorization;
using Azure.Functions.Authentication.Helpers;
using AzureFunctions.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FunctionAuthenticationExtensions
    {
        /// <summary>
        /// Registers services required by authentication services and configures <see cref="AuthenticationOptions"/>
        /// without affecting internal authentication services registered by Azure Functions Host
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
        public static AuthenticationBuilder AddFunctionAuthentication(this IServiceCollection services)
        {
            return services.AddFunctionAuthentication(options => { });
        }

        /// <summary>
        /// Registers services required by authentication services and configures <see cref="AuthenticationOptions"/>
        /// without affecting internal authentication services registered by Azure Functions Host
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureOptions">A delegate to configure <see cref="AuthenticationOptions"/>.</param>
        /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
        public static AuthenticationBuilder AddFunctionAuthentication(
            this IServiceCollection services,
            Action<AuthenticationOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);

            services.AddSingleton(new OptionsConfigurator<AuthenticationOptions>
            {
                Configure = configureOptions,
            });

            return new AuthenticationBuilder(services);
        }

        /// <summary>
        /// Adds authorization policy services to the specified <see cref="IServiceCollection" />
        /// without affecting internal authentication services registered by Azure Functions Host
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configure">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddFunctionAuthorization(
            this IServiceCollection services,
            Action<AuthorizationOptions> configure)
        {
            services.AddTransient<IPolicyEvaluator, FunctionPolicyEvaluator>();

            services.AddSingleton(new OptionsConfigurator<AuthorizationOptions> { Configure = configure });

            return services;
        }

        internal static AuthenticationOptions GetAuthenticationOptions(
            this IAuthenticationSchemeProvider schemeProvider)
        {
            // difficult times require difficult decisions ©
            return schemeProvider.GetFieldValue<AuthenticationOptions>("_options");
        }

        internal static AuthorizationOptions GetAuthorizationOptions(
            this IAuthorizationPolicyProvider policyProvider)
        {
            // difficult times require difficult decisions ©
            return policyProvider.GetFieldValue<AuthorizationOptions>("_options");
        }
    }
}
