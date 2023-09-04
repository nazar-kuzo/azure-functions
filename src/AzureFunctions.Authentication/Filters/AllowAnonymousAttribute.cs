using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied to does not require authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute, IFunctionFilter, IAllowAnonymous
    {
    }
}
