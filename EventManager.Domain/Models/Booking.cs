#nullable disable
using System.Text.Json.Serialization;
using EventManager.Domain.Common;

namespace EventManager.Domain.Models;

public class Booking
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; private set; }
    [JsonIgnore]
    public Event Event { get; set; }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    private Booking()
    {
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
