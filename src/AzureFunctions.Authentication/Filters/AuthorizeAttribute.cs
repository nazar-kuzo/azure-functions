﻿using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied to requires the specified authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute, IFunctionFilter, IAuthorizeData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class.
        /// </summary>
        public AuthorizeAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute"/> class with the specified policy.
        /// </summary>
        /// <param name="policy">The name of the policy to require for authorization.</param>
        public AuthorizeAttribute(string policy)
        {
            this.Policy = policy;
        }

        /// <summary>
        /// Gets or sets the policy name that determines access to the resource.
        /// </summary>
        public string Policy { get; set; }

        /// <summary>
        /// Gets or sets a comma delimited list of roles that are allowed to access the resource.
        /// </summary>
        public string Roles { get; set; }

        /// <summary>
        /// Gets or sets a comma delimited list of schemes from which user information is constructed.
        /// </summary>
        public string AuthenticationSchemes { get; set; }
    }
}
