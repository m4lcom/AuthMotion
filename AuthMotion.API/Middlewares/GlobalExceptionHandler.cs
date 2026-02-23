using AuthMotion.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AuthMotion.API.Middlewares;

/// <summary>
/// Centralized error handling implementing RFC 7807 (ProblemDetails).
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the exception with path context for better traceability in Docker logs
        logger.LogError(exception, "Unhandled exception occurred during request to {Path}", httpContext.Request.Path);

        int statusCode = exception switch
        {
            BaseException domainEx => domainEx.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == 500 ? "Internal Server Error" : exception.GetType().Name.Replace("Exception", ""),
            Detail = statusCode == 500 ? "An unexpected error occurred. Please try again later." : exception.Message,
            Instance = httpContext.Request.Path
        };

        // Add TraceId to allow matching frontend errors with backend logs
        problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}