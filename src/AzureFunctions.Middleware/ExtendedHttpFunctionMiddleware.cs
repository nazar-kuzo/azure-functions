using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AzureFunctions.Middleware.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Indexers;

[assembly: InternalsVisibleTo("AzureFunctions.Middleware.Extensions")]

namespace AzureFunctions.Middleware
{
    /// <summary>
    /// Middleware that extends functions Http Function pipeline.
    /// 
    /// </summary>
    internal class ExtendedHttpFunctionMiddleware
    {
        private const string ExtendedHttpFunctionMiddlewareRequestDelegate = "ExtendedHttpFunctionMiddlewareRequestDelegate";

        private readonly ConcurrentDictionary<string, IFunctionFilter[]> functionAuthorizationData = new();
        private readonly IWebJobsRouter webJobsRouter;
        private readonly IFunctionIndexLookup functionLookup;
        private readonly RequestDelegate application;

        public ExtendedHttpFunctionMiddleware(
            IWebJobsRouter webJobsRouter,
            IFunctionIndexLookup functionLookup,
            IApplicationBuilder applicationBuilder)
        {
            this.webJobsRouter = webJobsRouter;
            this.functionLookup = functionLookup;
            this.application = applicationBuilder
                .Use((context, defaultNext) =>
                {
                    // custom defined pipeline that concatenates with Http Function pipeline
                    if (context.Items.Remove(ExtendedHttpFunctionMiddlewareRequestDelegate, out object requestDelegate) &&
                        requestDelegate is RequestDelegate next)
                    {
                        return next(context);
                    }
                    else
                    {
                        return defaultNext(context);
                    }
                })
                .Build();
        }

        public virtual async Task Invoke(HttpContext context, RequestDelegate next)
        {
            // stores RequestDelegate in HttpContext in order to pickup by extended pipeline
            context.Items.Add(ExtendedHttpFunctionMiddlewareRequestDelegate, next);

            await SetEndpoint(context);

            await application.Invoke(context);

            await next(context);
        }

        private async Task SetEndpoint(HttpContext context)
        {
            var routeContext = new RouteContext(context);

            await webJobsRouter.RouteAsync(routeContext);

            if (routeContext.Handler != null)
            {
                var functionName = routeContext.Handler.Target.GetFieldValue<string>("functionName");

                var authorizeFilters = functionAuthorizationData.GetOrAdd(
                    functionName,
                    _ =>
                    {
                        var functionDescriptor = functionLookup.LookupByName(functionName).Descriptor;

                        return functionDescriptor
                            .GetPropertyValue<IEnumerable<IFunctionFilter>>("ClassLevelFilters")
                            .Concat(functionDescriptor.GetPropertyValue<IEnumerable<IFunctionFilter>>("MethodLevelFilters"))
                            .Where(filter => filter is IAuthorizeData || filter is IAllowAnonymous)
                            .ToArray();
                    });

                context.SetEndpoint(new Endpoint(
                    requestDelegate: null,
                    new EndpointMetadataCollection(authorizeFilters),
                    displayName: functionName));
            }
        }

        /// <summary>
        /// Creates dynamic proxy type that inherits from Microsoft.Azure.WebJobs.Script.Middleware.IJobHostHttpMiddleware in order to register service implementation.
        /// </summary>
        /// <param name="jobHostHttpMiddlewareService">IJobHostHttpMiddleware interface type</param>
        /// <returns>Dynamic proxy type that is subclass of <see cref="ExtendedHttpFunctionMiddleware"/> and inherits from IJobHostHttpMiddleware</returns>
        public static Type CreateTypeProxy(Type jobHostHttpMiddlewareService)
        {
            var middlewareBaseType = typeof(ExtendedHttpFunctionMiddleware);
            var middlewareBaseConstructor = middlewareBaseType.GetConstructors().First();
            var middlewareBaseConstructorParameters = middlewareBaseConstructor.GetParameters();
            var middlewareBaseConstructorParameterTypes = middlewareBaseConstructorParameters
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            var middlewareTypeBuilder = AssemblyBuilder
                .DefineDynamicAssembly(new AssemblyName("AzureFunctions.Middleware.Extensions"), AssemblyBuilderAccess.Run)
                .DefineDynamicModule("Core")
                .DefineType(
                    $"{middlewareBaseType.Name}Proxy",
                    TypeAttributes.NotPublic,
                    parent: middlewareBaseType,
                    interfaces: new[] { jobHostHttpMiddlewareService });

            var ilGenerator = middlewareTypeBuilder
                .DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    middlewareBaseConstructorParameterTypes)
                .GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);

            switch (middlewareBaseConstructorParameters.Length)
            {
                case 1:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;

                case 2:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;

                case 3:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;

                case 4:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    ilGenerator.Emit(OpCodes.Ldarg_S, middlewareBaseConstructorParameters[3].Name);
                    break;
            }

            ilGenerator.Emit(OpCodes.Call, middlewareBaseConstructor);
            ilGenerator.Emit(OpCodes.Nop);
            ilGenerator.Emit(OpCodes.Nop);
            ilGenerator.Emit(OpCodes.Ret);

            return middlewareTypeBuilder.CreateType();
        }
    }
}
