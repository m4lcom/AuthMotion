using AuthMotion.Infrastructure.Extensions;
using AuthMotion.API.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Register dependencies by layer
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// 2. Configure the HTTP request pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Generates the OpenAPI specification (openapi/v1.json)
    app.MapOpenApi();

    // Renders the aesthetic Scalar UI
    app.MapScalarApiReference();
}

app.UseRateLimiter();
app.UseHttpsRedirection();

// CORS policy must be injected after routing/redirection but before Auth
app.UseCors("FrontendCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 3. Apply pending EF Core migrations automatically on startup
await app.InitializeDatabaseAsync();

app.Run();