using AuthMotion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthMotion.Infrastructure.Persistence;
public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

    }