using BookingEntity = Booking.Domain.Models.Booking;
namespace Booking.Application.DTOs;

public record BookingDTO
{
    public Guid Id { get; }
    public Guid EventId { get; }
    public string Status { get; }
    public DateTime? ProcessedAt { get; }

    public BookingDTO(BookingEntity booking)
    {
        Id = booking.Id;
        EventId = booking.EventId;
        Status = booking.Status.ToString();
        ProcessedAt = booking.ProcessedAt;
    }
}
