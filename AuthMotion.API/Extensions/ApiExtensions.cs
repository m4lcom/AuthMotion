using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using AuthMotion.API.Middlewares;
using AuthMotion.Application.Services;
using AuthMotion.Application.Interfaces;
using AuthMotion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

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
        // Note: In a larger project, this should be moved to an AddApplicationServices extension within the Application layer.
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
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials(); // Required for HttpOnly cookies
            });
        });

        // Rate Limiting Configuration
        services.AddRateLimiter(options =>
        {
            // Return 429 Too Many Requests instead of 503
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Specific policy for password recovery endpoint
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

    // Extension method to apply database migrations and seed initial data
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Apply pending migrations
        await context.Database.MigrateAsync();

        // 2. Seed default data
        await DatabaseSeeder.SeedAsync(context);
    }
}