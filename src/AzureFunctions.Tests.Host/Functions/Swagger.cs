using System.Net.Http;
using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using AzureFunctions.Extensions.Swashbuckle.SwashBuckle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace AzureFunctions.Tests.Host.Functions
{
    public class Swagger
    {
        [SwaggerIgnore]
        [FunctionName(nameof(Swagger) + "-" + nameof(SwaggerJson))]
        public HttpResponseMessage SwaggerJson(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/json")] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swashBuckleClient)
        {
            return swashBuckleClient.CreateSwaggerJsonDocumentResponse(req);
        }

        [SwaggerIgnore]
        [FunctionName(nameof(Swagger) + "-" + nameof(SwaggerUI))]
        public HttpResponseMessage SwaggerUI(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/ui")] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swashBuckleClient)
        {
            return swashBuckleClient.CreateSwaggerUIResponse(req, "swagger/json");
        }

        [SwaggerIgnore]
        [FunctionName(nameof(Swagger) + "-" + nameof(SwaggerOAuthRedirect))]
        public HttpResponseMessage SwaggerOAuthRedirect(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger/oauth2-redirect")] HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swashBuckleClient)
        {
            return swashBuckleClient.CreateSwaggerOAuth2RedirectResponse(req);
        }
    }
}
