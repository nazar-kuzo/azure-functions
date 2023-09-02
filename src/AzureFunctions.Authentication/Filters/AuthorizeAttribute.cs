using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute, IFunctionFilter, IAuthorizeData
    {
        public AuthorizeAttribute()
        {
        }

        public AuthorizeAttribute(string policy)
        {
            this.Policy = policy;
        }

        public string Policy { get; set; }

        public string Roles { get; set; }

        public string AuthenticationSchemes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute, IFunctionFilter, IAllowAnonymous
    {
    }
}
