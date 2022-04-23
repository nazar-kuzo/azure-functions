using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request body.
    /// </summary>
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class FromBodyAttribute : Attribute
    {
    }
}
