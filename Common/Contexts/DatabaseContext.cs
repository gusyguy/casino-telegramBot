using Presentation.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Common.Contexts;

public class DatabaseContext : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder option)
        => option.UseSqlite("Data Source = sqlite3.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

}
