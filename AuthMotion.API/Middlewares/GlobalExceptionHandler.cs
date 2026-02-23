using AuthMotion.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AuthMotion.API.Middlewares;

/// <summary>
/// Centralized error handling middleware implementing RFC 7807 (ProblemDetails).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. We ALWAYS log the actual exception so it appears in Docker logs
        _logger.LogError(exception, "An unhandled exception occurred during the request.");

        // 2. Map domain exceptions to HTTP status codes
        int statusCode = exception switch
        {
            BaseException domainEx => domainEx.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        // 3. Format the response using the ProblemDetails standard
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == 500 ? "Internal Server Error" : exception.GetType().Name.Replace("Exception", ""),
            Detail = statusCode == 500 ? "An unexpected error occurred. Please try again later." : exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}