using CozyTts.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CozyTts.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            AppConflictException => (StatusCodes.Status409Conflict, "Request conflicts with current state"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            logger.LogInformation(exception, "Handled API exception.");
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
