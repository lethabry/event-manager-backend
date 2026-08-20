using BookingEntity = Booking.Domain.Models.Booking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.DataAccess.Configuration;

public class BookingConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("bookings").HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever().IsRequired().HasColumnName("id");
        builder.Property(e => e.EventId).IsRequired().HasColumnName("event_id");
        builder.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(250).HasColumnName("status");
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at");
    }
}
