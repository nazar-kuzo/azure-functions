using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AzureFunctions.ModelBinding.ModelBinding
{
    /// <summary>
    /// Allows to parse JSON values from <see cref="IFormCollection"/>.
    /// </summary>
    internal class JsonFormValueModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.BindingInfo.BindingSource == BindingSource.Form &&
                context.Metadata.ModelType != typeof(IFormFile) &&
                context.Metadata.ModelType != typeof(IFormFileCollection))
            {
                return new JsonFormValueModelBinder(
                    context.Services.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>());
            }

            return null;
        }
    }

    /// <summary>
    /// Allows to parse JSON values from <see cref="IFormCollection"/>.
    /// </summary>
    internal class JsonFormValueModelBinder : IModelBinder
    {
        private readonly JsonSerializerSettings serializerSettings;

        public JsonFormValueModelBinder(IOptions<MvcNewtonsoftJsonOptions> jsonOptions)
        {
            this.serializerSettings = jsonOptions.Value.SerializerSettings;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var form = bindingContext.HttpContext.Request.Form;

            if (form.ContainsKey(bindingContext.ModelName))
            {
                var formValue = JsonConvert.DeserializeObject(
                    form[bindingContext.ModelName],
                    bindingContext.ModelType,
                    serializerSettings);

                bindingContext.Result = ModelBindingResult.Success(formValue);
            }

            return Task.CompletedTask;
        }
    }
}
