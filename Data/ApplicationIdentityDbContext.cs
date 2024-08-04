using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogApplication.Data;

public class ApplicationIdentityDbContext : IdentityDbContext<User, Role , Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{

    public ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {  
            entity.ToTable(name: "User");
        });
        builder.Entity<Role>(entity =>
        {
            entity.ToTable(name: "Role");
        });
        builder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");

        });
        builder.Entity<UserClaim>(entity =>
        {
            entity.ToTable("UserClaim");

        });
        builder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("UserLogin");
            //in case you chagned the TKey type
            //  entity.HasKey(key => new { key.ProviderKey, key.LoginProvider });       
        });
        builder.Entity<RoleClaim>(entity =>
        {
            entity.ToTable("RoleClaim");

        });
        builder.Entity<UserToken>(entity =>
        {
            entity.ToTable("UserToken");
            //in case you chagned the TKey type
            // entity.HasKey(key => new { key.UserId, key.LoginProvider, key.Name });

        });
    }
}
