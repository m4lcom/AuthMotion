using Microsoft.EntityFrameworkCore;
using AuthMotion.Infrastructure.Persistence;
using AuthMotion.Infrastructure.Repositories;
using AuthMotion.Application.Interfaces;
using AuthMotion.Application.Services;
using AuthMotion.API.Middlewares;
using AuthMotion.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseExceptionHandler();

app.Run();


