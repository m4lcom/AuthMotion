using AuthMotion.Domain.Entities;

namespace AuthMotion.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> IsRegisteredAsync(string email);
    Task AddAsync(User user);
    Task<User?> GetByEmailAsync(string email);
}