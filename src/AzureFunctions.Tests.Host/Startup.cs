using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Settings;
using AzureFunctions.Tests.Host.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

[assembly: FunctionsStartup(typeof(AzureFunctions.Tests.Host.Startup))]

namespace AzureFunctions.Tests.Host
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            var jwtBearerScheme = "B2C";

            builder.Services
                .AddFunctionAuthentication(options =>
                {
                    options.DefaultScheme = jwtBearerScheme;
                    options.DefaultSignInScheme = jwtBearerScheme;
                    options.DefaultChallengeScheme = jwtBearerScheme;
                    options.DefaultAuthenticateScheme = jwtBearerScheme;
                })
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AuthenticationSettings:B2C"), jwtBearerScheme)
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            builder.Services.Configure<JwtBearerOptions>(jwtBearerScheme, options =>
            {
                options.TokenValidationParameters.AuthenticationType = jwtBearerScheme;
            });

            builder.Services.AddFunctionAuthorization(options =>
            {
                options.InvokeHandlersAfterFailure = false;

                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(jwtBearerScheme)
                    .RequireAssertion(context =>
                    {
                        var isUserAuthenticated = context.User?.Identity?.IsAuthenticated == true;

                        if (!isUserAuthenticated)
                        {
                            context.Fail();
                        }

                        return isUserAuthenticated;
                    })
                    .Build();

                options.AddPolicy("Email", builder => builder.RequireClaim(ClaimTypes.Email));
            });

            var authenticationSettings = configuration.GetSection("AuthenticationSettings:B2C");

            builder.Services.AddSwashBuckle(typeof(Startup).Assembly, docOptions =>
            {
                docOptions.Title = "AzureFunction Test Host";
                docOptions.SpecVersion = OpenApiSpecVersion.OpenApi3_0;
                docOptions.AddCodeParameter = false;
                docOptions.PrependOperationWithRoutePrefix = true;
                docOptions.ClientId = authenticationSettings.GetValue<string>("ClientId");
                docOptions.OAuth2RedirectPath = "http://localhost:7071/api/swagger/oauth2-redirect";
                docOptions.Documents = new[]
                {
                    new SwaggerDocument
                    {
                        Name = "v1",
                        Title = "AzureFunction Test Host",
                        Description = "AzureFunction Test Host",
                    },
                };

                docOptions.ConfigureSwaggerGen = options =>
                {
                    options.OperationFilterDescriptors.Clear();

                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "JWT Authorization header using the Bearer scheme.",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.OAuth2,
                        Scheme = jwtBearerScheme,
                        Reference = new OpenApiReference
                        {
                            Id = jwtBearerScheme,
                            Type = ReferenceType.SecurityScheme,
                        },
                        Flows = new OpenApiOAuthFlows
                        {
                            Implicit = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(authenticationSettings.GetValue<string>("Authority") + "/oauth2/v2.0/authorize"),
                                Scopes = new[] { authenticationSettings.GetValue<string>("UserScope") }.ToDictionary(
                                    scope => authenticationSettings.GetValue<string>("ApplicationIdUri") + "/" + scope,
                                    scope => scope),
                            },
                        },
                    };

                    options.AddSecurityDefinition(jwtBearerScheme, securityScheme);

                    options.TagActionsBy(description =>
                    {
                        return description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                            ? new[] { controllerActionDescriptor.ControllerTypeInfo.Name }
                            : new[] { description.ActionDescriptor.ToString() };
                    });

                    options.OperationFilter<SecurityFilter>(securityScheme);

                    options.EnableAnnotations(enableAnnotationsForInheritance: true, enableAnnotationsForPolymorphism: true);
                };
            });

            builder.Services
                .AddMvcCore()
                .AddFunctionModelBinding()
                .AddNewtonsoftJson(jsonOptions =>
                {
                    jsonOptions.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    jsonOptions.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    jsonOptions.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    jsonOptions.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                });

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddFunctionMiddleware(app =>
            {
                app.UseAuthentication();
                app.UseAuthorization();
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder
                .AddJsonFile(
                    path: Path.Combine(builder.GetContext().ApplicationRootPath, "local.settings.json"),
                    optional: true,
                    reloadOnChange: false)
                .AddUserSecrets<Startup>();
        }
    }
}
