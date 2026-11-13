using CRS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace CRS.Core.Data;

// The legacy system keeps its user directory in a separate database.
public sealed class ADUsersContext(DbContextOptions<ADUsersContext> options) : DbContext(options)
{
    public DbSet<DirectoryUser> Users => Set<DirectoryUser>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var user = model.Entity<DirectoryUser>();
        user.ToTable("ADUser"); user.HasKey(x => x.Id);
        user.Property(x => x.Id).HasColumnName("ID");
        user.Property(x => x.Email).HasColumnName("Mail");
    }
}
