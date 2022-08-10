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
            // add data annotations validation
            builder.Services.AddSingleton<FunctionModelBindingSourceBindingProvider>();
            builder.Services.AddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();

            builder.AddMvcOptions(mvcOptions =>
            {
                mvcOptions.ModelBinderProviders.Insert(0, new JsonFormValueModelBinderProvider());
            });

            return builder.AddDataAnnotations();
        }
    }
}
