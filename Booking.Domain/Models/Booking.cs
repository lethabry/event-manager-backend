#nullable disable
using Booking.Domain.Common;

namespace Booking.Domain.Models;

public class Booking
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; private set; }
    public Guid UserId { get; init; }

    public Booking(Guid eventId, Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private Booking()
    {
    }

    public bool Confirm()
    {
        if (Status is not BookingStatus.Pending)
        {
            return false;
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
        return true;
    }

    public bool Reject()
    {
        if (Status is not BookingStatus.Pending and not BookingStatus.Confirmed)
        {
            return false;
        }

        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
        return true;
    }

    public bool Cancel()
    {
        if (Status is not BookingStatus.Pending and not BookingStatus.Confirmed)
        {
            return false;
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
        return true;
    }
}
