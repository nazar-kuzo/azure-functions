# azure-functions-model-binding

Provides ASP.NET Core MVC model binding for Azure Functions v3


# Problem

Azure Function v3 is lacking of built-in ASP.NET Core MVC Model Binding attributes like [FromQuery], [FromBody], [FromForm].

## Solution

1. Expose custom Model binding attributes derived from ASP.NET Core MVC attributes
2. Provide custom extension that derives from "IExtensionConfigProvider"
3. Finish configuration of ASP.NET Core MVC services
4. Pass data from Azure Function binding attributes to ASP.NET Core MVC model binderand validator

# Example

1. Add MVC Core services by calling `builder.Services.AddMvcCore()`
2. Register Azure Function Model binding by calling `.AddFunctionModelBinding()`
3. Add [FromQuery]/[FromBody]/[FromForm] attributes to your function parameters
```
[FunctionName(QueryData)
public object QueryData(
    [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "data")] HttpRequest request,
    [FromQuery, Required] string filter,
    [FromBody] object content)
{
    // your code goes here
}
```
4. Enjoy 😄
