using System.Text.Json;
using EventManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Middleware;

public class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var isUnexpected = statusCode == StatusCodes.Status500InternalServerError && exception is not EventException;

        if (isUnexpected)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. Method: {Method}, Path: {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                statusCode);
        }

        var response = new ProblemDetails
        {
            Title = isUnexpected ? "Internal server error" : exception.Message,
            Status = statusCode
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => StatusCodes.Status409Conflict,
            EventValidationException => StatusCodes.Status400BadRequest,
            EventNotFoundException => StatusCodes.Status404NotFound,
            EventDeletionFailedException => StatusCodes.Status500InternalServerError,
            BookingNotFoundException => StatusCodes.Status404NotFound,
            NoAvailableSeatsException => StatusCodes.Status409Conflict,
            BookingPastEventException => StatusCodes.Status400BadRequest,
            ActiveBookingLimitException => StatusCodes.Status409Conflict,
            AccessDeniedException => StatusCodes.Status403Forbidden,
            UserValidationException => StatusCodes.Status400BadRequest,
            UserNotFoundException => StatusCodes.Status404NotFound,
            BookingStatusConflictException => StatusCodes.Status409Conflict,
            UserExistException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
