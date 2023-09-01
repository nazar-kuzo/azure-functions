using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.ModelBinding
{
    public class FunctionModelBindingOptions
    {
        public Func<ActionContext, Task> OnModelBinding { get; set; }

        public Func<ActionContext, ValidationProblemDetails, Task> OnModelBindingFailed { get; set; }
    }
}
