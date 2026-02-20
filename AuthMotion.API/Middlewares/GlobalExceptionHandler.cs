using AuthMotion.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace AuthMotion.API.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Manejamos nuestra excepción de negocio
        if (exception is ConflictException conflictException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = conflictException.Message },
                cancellationToken);

            return true; // Le decimos a .NET "Ya me encargué, no hagas más nada"
        }

        if (exception is UnauthorizedException unauthorizedException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = unauthorizedException.Message },
                cancellationToken);

            return true; // Le decimos a .NET "Ya me encargué, no hagas más nada"
        }

        // 2. Manejamos cualquier otro error inesperado (Bugs, Base de datos caída, etc)
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "An unexpected error occurred in the server." },
            cancellationToken);

        return true;
    }
}