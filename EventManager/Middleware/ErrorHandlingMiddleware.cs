using EventManager.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EventException ex)
        {
            await HandleExceptionAsync(context, ex, (int)ex.statusCode);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, 500);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, int statusCode)
    {
        _logger.LogError(
            exception,
            "Unhandled exception. Method: {Method}, Path: {Path}",
            context.Request.Method,
            context.Request.Path
        );

        var response = new ProblemDetails
        {
            Title = exception.Message,
            Status = statusCode
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(response);
    }
}