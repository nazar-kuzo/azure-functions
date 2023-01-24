# Azure Functions extensibility features

Provides Azure Functions extensibility features that we love using in ASP.NET Core applications

## AzureFunctions.Authentication

Provides Azure Functions friendly ASP.NET Core Authentication/Authorization

<https://www.nuget.org/packages/AzureFunctions.Authentication>

### Problem

Azure Function have ability to use Startup.cs class with Dependecy Injection same as ASP.NET Core applications, it is not working as expected out of the box though. The reason for that is default/internal (admin) features of Azure Functions Web Host that are protected with the exact same ASP.NET Core Authentication/Authorization registrations which will be overriden once you register yours and you'll start seeing such problem in Azure Portal ![image](https://user-images.githubusercontent.com/13677730/130435587-922ea1c7-8c0e-4985-84ca-d7bf5fb762d4.png).
Moreover, predefined Azure Functions `AuthorizationLevel` policies wont work neither.

### Root cause of a problem

Please read this issue to get more context: [https://github.com/Azure/azure-functions-host/issues/6805](https://github.com/Azure/azure-functions-host/issues/6805).

### Workaround

Register ASP.NET Core Authentication/Authorization in such a way that it is not replacing nor dropping existing configurations/schemas/handlers but extend it instead. This is achieved with "Dynamic schema registration", see example here: [https://github.com/aspnet/AuthSamples/tree/master/samples/DynamicSchemes](https://github.com/aspnet/AuthSamples/tree/master/samples/DynamicSchemes).

### Solution

1. Expose custom Authentication/Authorization builder extensions that dont override existing one but registers all needed services
2. Provide custom extension that derives from `IExtensionConfigProvider`
3. Re-configure already configured Authentication/Authorization by Azure Functions
4. Dynamically inject new authentication schema and handler since `Bearer` schema is used by Azure Functions with their handler
5. Override `IAuthorizationHandlerProvider` to merge Azure Functions handlers with application handlers

### Example

1. Add/replace existing AddAuthentication/AddAuthorization extension methods to AddFunctionAuthentication/AddFunctionAuthorization
2. Register `IAuthorizationHandler` handlers
3. Inject all needed services `IAuthenticationSchemeProvider/IAuthorizationPolicyProvider/IPolicyEvaluator` to authenticate & authorize request inside function. Alternative, encourage you to try nice package out there to simplify this process [https://www.nuget.org/packages/DarkLoop.Azure.Functions.Authorize](https://www.nuget.org/packages/DarkLoop.Azure.Functions.Authorize)
4. Enjoy 😄

### Important

```diff
! Do not try to use the "Bearer" scheme name since it is already used by AzureFunction host internally, provide any other name like: "CustomBearer", "B2B", etc.
```

### Code snippet

```c#
    private static void ConfigureAuthorization(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        builder.Services.AddFunctionAuthentication(
            configuration,
            defaultAuthenticationScheme: "CustomBearer");

        builder.Services.AddFunctionAuthorization(
            configuration,
            configureDefaultPolicy: policy => policy.RequireRole("Admin"),
            configureOptions: options =>
            {
                options.AddPolicy(
                    "Organizatiuon",
                    builder => builder
                        .Combine(options.DefaultPolicy)
                        .AddRequirements(new OrganizationRequirement()));
            });

        builder.Services.AddScoped<IAuthorizationHandler, OrganizationAuthorizationHandler>();
    }
```

## AzureFunctions.MvcModelBinding

Provides ASP.NET Core MVC model binding for Azure Functions

<https://www.nuget.org/packages/AzureFunctions.MvcModelBinding>

### Problem

Azure Function is lacking of built-in ASP.NET Core MVC Model Binding attributes like [FromQuery], [FromBody], [FromForm].

### Solution

1. Expose custom Model binding attributes derived from ASP.NET Core MVC attributes
1. Provide custom extension that derives from `IExtensionConfigProvider`
1. Finish configuration of ASP.NET Core MVC services
1. Pass data from Azure Function binding attributes to ASP.NET Core MVC model binderand validator

### Example

1. Add MVC Core services by calling `builder.Services.AddMvcCore()`
1. Register Azure Function Model binding by calling `.AddFunctionModelBinding()`
1. Add [FromQuery]/[FromBody]/[FromForm] attributes to your function parameters

    ```c#
    [FunctionName("QueryData")]
    public object QueryData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "data")] HttpRequest request,
        [FromQuery, Required] string filter,
        [FromBody] object content)
    {
        // your code goes here
    }
    ```

1. Enjoy 😄
