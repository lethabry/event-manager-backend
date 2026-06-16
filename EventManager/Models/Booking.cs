#nullable disable
using EventManager.Common;

namespace EventManager.Models;

public class Booking
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; private set; }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;    
    }

    public bool Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            return false;
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
        return true;
    }

    public bool Reject()
    {
        if (Status != BookingStatus.Pending)
        {
            return false;
        }

        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        return true;
    }
}

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
        Status = Enum.GetName(typeof(BookingStatus), booking.Status)?.ToLower();
        ProcessedAt = booking.ProcessedAt;
    }
}