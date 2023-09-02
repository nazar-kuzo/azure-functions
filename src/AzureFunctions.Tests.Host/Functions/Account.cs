using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace AzureFunctions.Tests.Host.Functions
{
    [Authorize]
    public class Account
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public Account(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        [Authorize(Policy = "Email")]
        [FunctionName(nameof(Account) + "-" + nameof(GetUser))]
        public object GetUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "account/user")] HttpRequest request)
        {
            var user = this.httpContextAccessor.HttpContext.User;

            return new
            {
                Name = user.FindFirst("name").Value,
            };
        }

        [AllowAnonymous]
        [FunctionName(nameof(Account) + "-" + nameof(Login))]
        public object Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "account/login")] HttpRequest request)
        {
            return "Nice try";
        }
    }
}
