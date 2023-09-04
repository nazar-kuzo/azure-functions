## AzureFunctions.Middleware

Provides Azure Functions friendly ASP.NET Core Middleware support

[https://www.nuget.org/packages/AzureFunctions.Middleware](https://www.nuget.org/packages/AzureFunctions.Middleware)

### Problem

Azure Function is lacking of built-in ASP.NET Core Middleware pipeline that is handy for variety of use-cases like .UseAuthentication(), .UseAuthorization(), etc.

### Example

```c#
public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddFunctionMiddleware(app =>
        {
            app.UseAuthentication();
            app.UseAuthorization();
        });
    }
```

