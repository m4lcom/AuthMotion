using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using AuthMotion.API.Middlewares;
using AuthMotion.Application.Services;
using AuthMotion.Application.Interfaces;

namespace AuthMotion.API.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Application Services
        services.AddScoped<IAuthService, AuthService>();

        // Middlewares and API Features
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi();

        // CORS Configuration for Next.js Frontend
        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCorsPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:3000") // TODO: Pasar esto a appsettings.json en producción
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for HttpOnly cookies
            });
        });

        // Rate Limiting Configuration
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("PasswordRecovery", limiterOptions =>
            {
                limiterOptions.PermitLimit = 3;
                limiterOptions.Window = TimeSpan.FromMinutes(15);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        return services;
    }
}