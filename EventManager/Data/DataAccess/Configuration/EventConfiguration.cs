using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace EventManager.Data.DataAccess.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events").HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever().IsRequired().HasColumnName("id");
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired().HasColumnName("title");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.StartAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("start_at");
        builder.Property(e => e.EndAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("end_at");
        builder.Property(e => e.TotalSeats).IsRequired().HasColumnName("total_seats");
        builder.Property(e => e.AvailableSeats).IsRequired().HasColumnName("available_seats");
        builder.HasMany(e => e.Bookings).WithOne(b => b.Event).HasForeignKey(b => b.EventId);
    }
}
