using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventManager.Presentation.OpenApi;

public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Concat(context.MethodInfo.GetCustomAttributes(true))
            ?? context.MethodInfo.GetCustomAttributes(true);

        if (attributes.OfType<AllowAnonymousAttribute>().Any() ||
            !attributes.OfType<AuthorizeAttribute>().Any())
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", context.Document)] = []
            }
        ];
    }
}
