using AuthMotion.Infrastructure.Extensions;
using AuthMotion.API.Extensions;
using Scalar.AspNetCore;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// 1. Register dependencies by layer
// Extension methods defined in Infrastructure and API layers
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// 2. Configure the HTTP request pipeline
// Global Exception Handler must be the first to catch errors from subsequent middlewares
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Generates the OpenAPI specification (openapi/v1.json)
    app.MapOpenApi();

    // Renders the modern and aesthetic Scalar UI for API documentation
    app.MapScalarApiReference();
}

// Transport Security - Force HTTPS redirection
app.UseHttpsRedirection();

// IMPORTANT: CORS must be placed before RateLimiting and Auth
// This ensures the browser receives proper headers even on rejected requests
app.UseCors("FrontendCorsPolicy");

// Authentication (Who are you?) and Authorization (What can you do?)
app.UseAuthentication();
app.UseAuthorization();

// Rate Limiter is placed after Auth to allow limiting based on User Identity
app.UseRateLimiter();

// Map Controller endpoints
app.MapControllers();

// 3. Database Initialization
// Automatic migrations are only executed in Development to prevent race conditions in Docker/Production
if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
}

app.Run();