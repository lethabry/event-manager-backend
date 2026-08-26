using EventEntity = Event.Domain.Models.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Event.Infrastructure.DataAccess.Configuration;

public class EventConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("events").HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever().IsRequired().HasColumnName("id");
        builder.Property(e => e.Title).HasMaxLength(250).IsRequired().HasColumnName("title");
        builder.Property(e => e.Description).HasMaxLength(2000).HasColumnName("description");
        builder.Property(e => e.StartAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("start_at");
        builder.Property(e => e.EndAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("end_at");
        builder.Property(e => e.TotalSeats).IsRequired().HasColumnName("total_seats");
        builder.Property(e => e.AvailableSeats).IsRequired().HasColumnName("available_seats");
    }
}
