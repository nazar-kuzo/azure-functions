using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AzureFunctions.Tests.Host.Swagger
{
    /// <summary>
    /// Swagger operation filter that provides
    /// authorization security requirement based on [Authorize] attribute.
    /// </summary>
    internal class SecurityFilter : IOperationFilter
    {
        private readonly OpenApiSecurityScheme securityScheme;

        public SecurityFilter(OpenApiSecurityScheme securityScheme)
        {
            this.securityScheme = securityScheme;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authorizeAttributes = context
                .ApiDescription
                .CustomAttributes()
                .OfType<AuthorizeAttribute>()
                .ToList();

            var anonymousAttribute = context
                .ApiDescription
                .CustomAttributes()
                .OfType<AllowAnonymousAttribute>()
                .FirstOrDefault();

            if (authorizeAttributes.Any() && anonymousAttribute == null)
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    { this.securityScheme, new List<string>() },
                });

                var policies = authorizeAttributes
                    .Select(auth => auth.Policy).Where(policy => !string.IsNullOrEmpty(policy))
                    .ToList();

                var roles = authorizeAttributes
                    .Where(auth => !string.IsNullOrEmpty(auth.Roles))
                    .SelectMany(auth => auth.Roles.Split(",", System.StringSplitOptions.RemoveEmptyEntries))
                    .ToList();

                operation.Description ??= string.Empty;

                if (roles.Any())
                {
                    operation.Description += $"Authorized roles:\n" + string.Join("\n\n", roles.Select(role => $"* `{role}`"));
                }

                if (policies.Any())
                {
                    operation.Description = $"Authorized policies:\n" + string.Join("\n\n", policies.Select(policy => $"* `{policy}`"));
                }
            }
        }
    }
}
