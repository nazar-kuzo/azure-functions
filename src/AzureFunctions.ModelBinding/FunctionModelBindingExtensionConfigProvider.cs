using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Bindings = Microsoft.Azure.WebJobs.Extensions.Http;
using HostBindings = Microsoft.Azure.WebJobs.Host.Bindings;

namespace AzureFunctions.ModelBinding
{
    internal class FunctionModelBindingExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly FunctionModelBindingSourceBindingProvider bindingSourceBindingProvider;

        public FunctionModelBindingExtensionConfigProvider(
            FunctionModelBindingSourceBindingProvider bindingSourceBindingProvider)
        {
            this.bindingSourceBindingProvider = bindingSourceBindingProvider ?? throw new InvalidOperationException(
                $"ModelBinding services are not registered." +
                $"Please call {nameof(MvcFunctionModelBindingExtensions.AddFunctionModelBinding)}");
        }

        public void Initialize(ExtensionConfigContext context)
        {
            context
                .AddBindingRule<Bindings.FromQueryAttribute>()
                .Bind(this.bindingSourceBindingProvider);

            context
                .AddBindingRule<Bindings.FromRouteAttribute>()
                .Bind(this.bindingSourceBindingProvider);

            context
                .AddBindingRule<Bindings.FromBodyAttribute>()
                .Bind(this.bindingSourceBindingProvider);

            context
                .AddBindingRule<Bindings.FromFormAttribute>()
                .Bind(this.bindingSourceBindingProvider);
        }

        internal class FunctionModelBindingSourceBindingProvider : HostBindings.IBindingProvider
        {
            private static readonly PropertyInfo[] FormOptionProperties = typeof(FormOptions).GetProperties();

            private readonly IHttpContextAccessor httpContextAccessor;
            private readonly ParameterBinder parameterBinder;
            private readonly IModelBinderFactory modelBinderFactory;
            private readonly IModelMetadataProvider metadataProvider;

            public FunctionModelBindingSourceBindingProvider(
                IHttpContextAccessor httpContextAccessor,
                ParameterBinder parameterBinder,
                IModelBinderFactory modelBinderFactory,
                IModelMetadataProvider metadataProvider)
            {
                if (metadataProvider is EmptyModelMetadataProvider)
                {
                    throw new InvalidOperationException($"Please register '{nameof(DefaultModelMetadataProvider)}' instead to enable model validation");
                }

                this.httpContextAccessor = httpContextAccessor;
                this.parameterBinder = parameterBinder;
                this.modelBinderFactory = modelBinderFactory;
                this.metadataProvider = metadataProvider;
            }

            public Task<HostBindings.IBinding> TryCreateAsync(HostBindings.BindingProviderContext context)
            {
                var bindingSourceContext = this.GetBindingSourceContext(context);

                return Task.FromResult(new BindingSourceBinding(
                    this.parameterBinder,
                    this.modelBinderFactory.CreateBinder(bindingSourceContext.BinderContext),
                    bindingSourceContext,
                    this.httpContextAccessor) as HostBindings.IBinding);
            }

            private BindingSourceContext GetBindingSourceContext(HostBindings.BindingProviderContext context)
            {
                var bindingSourceAttribute = context.Parameter.CustomAttributes
                    .First(attr => attr.AttributeType.CustomAttributes
                        .Any(tag => tag.AttributeType == typeof(Microsoft.Azure.WebJobs.Description.BindingAttribute)));

                var propertyName = bindingSourceAttribute
                    .NamedArguments
                    .FirstOrDefault(arg => arg.MemberName == nameof(Bindings.IModelNameProvider.Name))
                    .TypedValue
                    .Value
                    ?.ToString() ?? context.Parameter.Name;

                var bindingSourceContext = default(BindingSourceContext);

                if (bindingSourceAttribute.AttributeType == typeof(Bindings.FromBodyAttribute))
                {
                    bindingSourceContext = new BindingSourceContext
                    {
                        Source = BindingSource.Body,
                        ParameterName = string.Empty,
                        ValueProviderFactory = actionContext => new RouteValueProvider(
                            BindingSource.Path,
                            new Microsoft.AspNetCore.Routing.RouteValueDictionary()),
                    };
                }
                else if (bindingSourceAttribute.AttributeType == typeof(Bindings.FromQueryAttribute))
                {
                    bindingSourceContext = new BindingSourceContext
                    {
                        Source = BindingSource.Query,
                        ParameterName = propertyName,
                        ValueProviderFactory = actionContext => new QueryStringValueProvider(
                            BindingSource.Query,
                            actionContext.HttpContext.Request.Query,
                            CultureInfo.InvariantCulture),
                    };
                }
                else if (bindingSourceAttribute.AttributeType == typeof(Bindings.FromRouteAttribute))
                {
                    bindingSourceContext = new BindingSourceContext
                    {
                        Source = BindingSource.Path,
                        ParameterName = propertyName,
                        ValueProviderFactory = actionContext => new RouteValueProvider(
                            BindingSource.Path,
                            actionContext.HttpContext.Request.RouteValues,
                            CultureInfo.InvariantCulture),
                    };
                }
                else if (bindingSourceAttribute.AttributeType == typeof(Bindings.FromFormAttribute))
                {
                    var overridenFormOptions = context.Parameter
                        .Member
                        .CustomAttributes
                        .FirstOrDefault(attribute => attribute.AttributeType == typeof(RequestFormLimitsAttribute))
                        ?.NamedArguments ?? new List<CustomAttributeNamedArgument>();

                    bindingSourceContext = new BindingSourceContext
                    {
                        Source = BindingSource.Form,
                        ParameterName = propertyName,
                        ValueProviderFactory = actionContext =>
                        {
                            if (actionContext.HttpContext.Request.HasFormContentType)
                            {
                                var defaultFormOptions = actionContext.HttpContext.RequestServices
                                    .GetRequiredService<IOptions<FormOptions>>()
                                    .Value;

                                var formOptions = new FormOptions();

                                foreach (var property in FormOptionProperties)
                                {
                                    var overridenProperty = overridenFormOptions
                                        .FirstOrDefault(propertyInfo => propertyInfo.MemberName == property.Name);

                                    property.SetValue(formOptions, overridenProperty.TypedValue.Value ?? property.GetValue(defaultFormOptions));
                                }

                                try
                                {
                                    return new FormValueProvider(
                                        BindingSource.Form,
                                        new FormFeature(actionContext.HttpContext.Request, formOptions).ReadForm(),
                                        CultureInfo.InvariantCulture);
                                }
                                catch (Exception ex)
                                {
                                    SendFormattedResponseAsync(
                                        actionContext.HttpContext,
                                        new ValidationProblemDetails(new Dictionary<string, string[]>
                                        {
                                            { propertyName, new[] { ex.Message } },
                                        }))
                                        .GetAwaiter()
                                        .GetResult();

                                    throw;
                                }
                            }

                            return null;
                        },
                    };
                }
                else
                {
                    throw new NotSupportedException();
                }

                bindingSourceContext.Parameter = context.Parameter;

                var avoidSettingBinderModelName = bindingSourceContext.Source == BindingSource.Body ||
                    (bindingSourceContext.Source == BindingSource.Query &&
                    context.Parameter.ParameterType.IsClass &&
                    !context.Parameter.ParameterType.FullName.StartsWith("System"));

                bindingSourceContext.BinderContext = new ModelBinderFactoryContext()
                {
                    Metadata = (this.metadataProvider as DefaultModelMetadataProvider).GetMetadataForParameter(context.Parameter),
                    BindingInfo = new BindingInfo
                    {
                        // avoid setting parameter name on body to avoid redundant prefix
                        BinderModelName = avoidSettingBinderModelName
                            ? string.Empty
                            : bindingSourceContext.ParameterName,
                        BindingSource = bindingSourceContext.Source,
                    },
                };

                if (bindingSourceContext.Source == BindingSource.Body)
                {
                    bindingSourceContext.BinderContext.BindingInfo.RequestPredicate = actionContext =>
                    {
                        return !actionContext.HttpContext.Request.HasFormContentType;
                    };
                }

                return bindingSourceContext;
            }

            private static Task SendFormattedResponseAsync<TContent>(
                HttpContext httpContext,
                TContent content,
                int statusCode = StatusCodes.Status400BadRequest)
            {
                var formatterSelector = httpContext.RequestServices.GetRequiredService<OutputFormatterSelector>();
                var writerFactory = httpContext.RequestServices.GetRequiredService<IHttpResponseStreamWriterFactory>();

                var writeContext = new OutputFormatterWriteContext(
                    httpContext,
                    (stream, encoding) => writerFactory.CreateWriter(stream, encoding),
                    objectType: content.GetType(),
                    @object: content);

                var formatter = formatterSelector.SelectFormatter(
                    writeContext,
                    httpContext.RequestServices.GetRequiredService<IOptions<MvcOptions>>().Value.OutputFormatters,
                    new MediaTypeCollection());

                httpContext.Response.StatusCode = statusCode;

                return formatter.WriteAsync(writeContext);
            }

            private class BindingSourceBinding : HostBindings.IBinding
            {
                private readonly ParameterBinder parameterBinder;
                private readonly IModelBinder modelBinder;
                private readonly BindingSourceContext bindingSourceContext;
                private readonly IHttpContextAccessor httpContextAccessor;

                public BindingSourceBinding(
                    ParameterBinder parameterBinder,
                    IModelBinder modelBinder,
                    BindingSourceContext bindingSourceContext,
                    IHttpContextAccessor httpContextAccessor)
                {
                    this.parameterBinder = parameterBinder;
                    this.modelBinder = modelBinder;
                    this.bindingSourceContext = bindingSourceContext;
                    this.httpContextAccessor = httpContextAccessor;
                }

                public bool FromAttribute => true;

                public ParameterDescriptor ToParameterDescriptor() => new ParameterDescriptor();

                public Task<HostBindings.IValueProvider> BindAsync(object value, HostBindings.ValueBindingContext context)
                {
                    throw new NotSupportedException("Value should never be provided explicitly");
                }

                public Task<HostBindings.IValueProvider> BindAsync(HostBindings.BindingContext context)
                {
                    var modelState = new ModelStateDictionary();

                    // remove value from route data to force explicit attribute binding
                    if (this.bindingSourceContext.Source == BindingSource.Path &&
                        this.httpContextAccessor.HttpContext.Items["MS_AzureWebJobs_HttpRouteData"] is Dictionary<string, object> routeData &&
                        routeData.ContainsKey(this.bindingSourceContext.ParameterName))
                    {
                        routeData.Remove(this.bindingSourceContext.ParameterName);
                    }

                    var bindingContext = new DefaultModelBindingContext
                    {
                        IsTopLevelObject = true,
                        ModelState = modelState,
                        ModelName = this.bindingSourceContext.ParameterName,
                        FieldName = this.bindingSourceContext.BinderContext.BindingInfo.BinderModelName,
                        ModelMetadata = this.bindingSourceContext.BinderContext.Metadata,
                        BindingSource = this.bindingSourceContext.BinderContext.BindingInfo.BindingSource,
                        ValidationState = new ValidationStateDictionary(),
                        ActionContext = new ActionContext(
                            this.httpContextAccessor.HttpContext,
                            new Microsoft.AspNetCore.Routing.RouteData(),
                            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
                            modelState),
                    };

                    if (this.bindingSourceContext.ValueProviderFactory.Invoke(bindingContext.ActionContext) is IValueProvider valueProvider)
                    {
                        bindingContext.ValueProvider = valueProvider;
                    }

                    return Task.FromResult(new BindingSourceValueProvider(
                        this.parameterBinder,
                        this.modelBinder,
                        this.bindingSourceContext,
                        bindingContext) as HostBindings.IValueProvider);
                }

                private class BindingSourceValueProvider : HostBindings.IValueProvider
                {
                    private readonly ParameterBinder parameterBinder;
                    private readonly IModelBinder modelBinder;
                    private readonly BindingSourceContext bindingSourceContext;
                    private readonly ModelBindingContext modelBindingContext;

                    public BindingSourceValueProvider(
                        ParameterBinder parameterBinder,
                        IModelBinder modelBinder,
                        BindingSourceContext bindingSourceContext,
                        ModelBindingContext modelBindingContext)
                    {
                        this.parameterBinder = parameterBinder;
                        this.modelBinder = modelBinder;
                        this.bindingSourceContext = bindingSourceContext;
                        this.modelBindingContext = modelBindingContext;
                    }

                    public Type Type => this.modelBindingContext.ModelType;

                    public string ToInvokeString() => string.Empty;

                    public async Task<object> GetValueAsync()
                    {
                        this.modelBindingContext.Result = await this.parameterBinder.BindModelAsync(
                            this.modelBindingContext.ActionContext,
                            this.modelBinder,
                            this.modelBindingContext.ValueProvider,
                            new Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor
                            {
                                Name = this.modelBindingContext.ModelName,
                                ParameterType = this.modelBindingContext.ModelType,
                                BindingInfo = bindingSourceContext.BinderContext.BindingInfo,
                            },
                            this.modelBindingContext.ModelMetadata,
                            value: null);

                        if (this.modelBindingContext.ModelState.IsValid)
                        {
                            var value = this.modelBindingContext.Result.IsModelSet
                                ? this.modelBindingContext.Result.Model
                                : this.bindingSourceContext.Parameter.HasDefaultValue
                                    ? this.bindingSourceContext.Parameter.DefaultValue
                                    : GetDefaultValueForType(this.bindingSourceContext.Parameter.ParameterType);

                            if (this.modelBindingContext.HttpContext.Items["MS_AzureWebJobs_HttpRouteData"] is Dictionary<string, object> routeData)
                            {
                                // updating AzureFunction route values to bypass their default parameter binding by "HttpTriggerAttributeBindingProvider"
                                if (routeData.ContainsKey(this.modelBindingContext.ModelName))
                                {
                                    routeData[this.modelBindingContext.ModelName] = value;
                                }
                            }

                            // store bound request body as route paramater for later value reuse
                            if (bindingSourceContext.BinderContext.BindingInfo.BindingSource == BindingSource.Body)
                            {
                                this.modelBindingContext.HttpContext.Items["MS_AzureFunctions_HttpRequestBody"] = value;
                            }

                            return value;
                        }
                        else
                        {
                            var validationProblemDetails = new ValidationProblemDetails(this.modelBindingContext.ModelState);

                            SendFormattedResponseAsync(
                                this.modelBindingContext.ActionContext.HttpContext,
                                validationProblemDetails)
                                .GetAwaiter()
                                .GetResult();

                            var validationException = new ValidationException(validationProblemDetails.Title);

                            validationException.Data.Add("Errors", validationProblemDetails);

                            throw validationException;
                        }
                    }
                }

                private static object GetDefaultValueForType(Type type)
                {
                    if (type.IsValueType)
                    {
                        return Activator.CreateInstance(type);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            private class BindingSourceContext
            {
                public BindingSource Source { get; set; }

                public string ParameterName { get; set; }

                public Func<ActionContext, IValueProvider> ValueProviderFactory { get; set; }

                public ModelBinderFactoryContext BinderContext { get; set; }

                public ParameterInfo Parameter { get; set; }
            }
        }
    }
}
