using EventManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventManager.Infrastructure.DataAccess.Configuration;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings").HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever().IsRequired().HasColumnName("id");
        builder.Property(e => e.EventId).IsRequired().HasColumnName("event_id");
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(250).HasColumnName("status");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
        builder.Property(e => e.ProcessedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("processed_at");

        builder.HasOne(d => d.Event).WithMany(p => p.Bookings).HasForeignKey(d => d.EventId);
    }
}
