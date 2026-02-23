using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AuthMotion.Infrastructure.Persistence;
using AuthMotion.Infrastructure.Repositories;
using AuthMotion.Infrastructure.Services;
using AuthMotion.Infrastructure.Authentication;
using AuthMotion.Application.Interfaces;
using Microsoft.AspNetCore.Builder; // Necesario para WebApplication

namespace AuthMotion.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new Exception("JWT Secret missing");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            // SOLUCIÓN: Le enseñamos a .NET a extraer el token de la cookie "jwt"
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.ContainsKey("jwt"))
                    {
                        context.Token = context.Request.Cookies["jwt"];
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie()
        .AddGoogle(options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ClientId = configuration["Authentication:Google:ClientId"] ?? throw new Exception("Google ClientId missing");
            options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? throw new Exception("Google ClientSecret missing");
            options.CallbackPath = "/signin-google";
        });

        return services;
    }

    // ARREGLO ARQUITECTÓNICO: Movemos el inicializador de BD a la capa que conoce el DbContext
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);
    }
}