using Microsoft.EntityFrameworkCore;
using Users.DataAccess.Entites;

namespace Users.DataAccess;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<UserEntity> Users { get; set; }
}