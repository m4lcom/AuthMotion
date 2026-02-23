using Microsoft.EntityFrameworkCore;
using AuthMotion.Application.Interfaces;
using AuthMotion.Domain.Entities;
using AuthMotion.Infrastructure.Persistence;

namespace AuthMotion.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Checks if an email already exists in the database.
    /// </summary>
    public async Task<bool> IsRegisteredAsync(string email)
    {
        return await _dbContext.Users.AnyAsync(u => u.Email == email);
    }

    /// <summary>
    /// Persists a new user to the database.
    /// </summary>
    public async Task AddAsync(User user)
    {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Finds a user by email. 
    /// Note: Entity is tracked by default so updates can be applied and saved easily.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Updates user information and commits changes to the database.
    /// </summary>
    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }
}