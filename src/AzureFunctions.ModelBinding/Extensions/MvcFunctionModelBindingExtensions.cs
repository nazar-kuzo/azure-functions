using System;
using System.ComponentModel.DataAnnotations;
using AzureFunctions.ModelBinding;
using AzureFunctions.ModelBinding.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using static AzureFunctions.ModelBinding.FunctionModelBindingExtensionConfigProvider;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcFunctionModelBindingExtensions
    {
        public static IMvcCoreBuilder AddFunctionModelBinding(this IMvcCoreBuilder builder)
        {
            return builder.AddFunctionModelBinding(options => { });
        }

        public static IMvcCoreBuilder AddFunctionModelBinding(
            this IMvcCoreBuilder builder,
            Action<FunctionModelBindingOptions> configureOptions)
        {
            // add data annotations validation
            builder.Services.AddSingleton<FunctionModelBindingSourceBindingProvider>();
            builder.Services.AddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();

            builder.AddMvcOptions(mvcOptions =>
            {
                mvcOptions.ModelBinderProviders.Insert(0, new JsonFormValueModelBinderProvider());
            });

            builder.Services.Configure<FunctionModelBindingOptions>(modelBindingOptions =>
            {
                modelBindingOptions.OnModelBindingFailed = async (actionContext, validationProblemDetails) =>
                {
                    await FunctionModelBindingSourceBindingProvider.SendFormattedResponseAsync(actionContext.HttpContext, validationProblemDetails);

                    var validationException = new ValidationException(validationProblemDetails.Title);

                    validationException.Data.Add("Errors", validationProblemDetails);

                    throw validationException;
                };

                configureOptions(modelBindingOptions);
            });

            return builder.AddDataAnnotations();
        }
    }
}
