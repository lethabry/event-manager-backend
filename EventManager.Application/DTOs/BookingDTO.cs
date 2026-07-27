using EventManager.Domain.Models;
namespace EventManager.Application.DTOs;

public record BookingDTO
{
    public Guid Id { get; }
    public Guid EventId { get; }
    public string Status { get; }
    public DateTime? ProcessedAt { get; }

    public BookingDTO(Booking booking)
    {
        Id = booking.Id;
        EventId = booking.EventId;
        Status = booking.Status.ToString();
        ProcessedAt = booking.ProcessedAt;
    }
}
