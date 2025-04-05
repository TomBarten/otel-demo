using Microsoft.EntityFrameworkCore;

namespace Coffee.Api;

public sealed class CoffeeDbContext : DbContext
{
    public DbSet<Coffee> Coffees { get; set; }
    
    public CoffeeDbContext(DbContextOptions<CoffeeDbContext> options) 
        : base(options)
    {
    }
}