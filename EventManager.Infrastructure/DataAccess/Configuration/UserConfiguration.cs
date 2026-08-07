using EventManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EventManager.Infrastructure.DataAccess.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users").HasKey(e => e.Id);
        builder.HasIndex(e => e.Login).IsUnique();

        builder.Property(e => e.Id).ValueGeneratedNever().IsRequired().HasColumnName("id");
        builder.Property(e => e.Login).IsRequired().HasMaxLength(250).HasColumnName("login");
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(250).HasColumnName("role");
        builder.Property(e => e.PasswordHash).HasConversion<string>().HasMaxLength(250).HasColumnName("password_hash");

        builder.HasMany(d => d.Bookings).WithOne(p => p.User).HasForeignKey(d => d.UserId);
    }
}
