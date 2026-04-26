using System.Text.Json;
using EventManager.Data;

namespace EventManager.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            await HandleExceptionAsync(context, ex, 400);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, 404);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, 500);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, int statusCode)
    {
        var response = new ErrorResponse
        {
            Title = exception.Message,
            StatusCode = statusCode
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}