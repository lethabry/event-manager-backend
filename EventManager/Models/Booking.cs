#nullable disable
using EventManager.Common;

namespace EventManager.Models;

public class Booking
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; private set; }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public bool TrySetDateOfProcessing(DateTime date)
    {
        if (ProcessedAt.HasValue)
        {
            return false;
        }

        ProcessedAt = date;
        return true;
    }
}