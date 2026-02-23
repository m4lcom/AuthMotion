using AuthMotion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthMotion.Infrastructure.Persistence;

// El constructor pasa a la firma de la clase. Chau a las líneas extra.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
    }
}