using AuthMotion.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AuthMotion.Domain.Enums;

namespace AuthMotion.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var adminUser = new User
        {
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234"),
            IsEmailVerified = true,
            Role = Role.Admin
        };
        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();
    }
}
