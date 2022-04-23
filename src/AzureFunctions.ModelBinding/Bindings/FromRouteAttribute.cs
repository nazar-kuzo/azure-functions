using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using route-data from the current request.
    /// </summary>
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class FromRouteAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
