#nullable disable
using System.Data;
using EventManager.Common;
using Microsoft.OpenApi;

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

    public bool UpdateBookingStatus(BookingStatus status)
    {
        if (Status != BookingStatus.Pending)
        {
            return false;
        }

        Status = status;
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

    public BookingDTO(Guid id, Guid eventId, BookingStatus status, DateTime? processedAt)
    {
        Id = id;
        EventId = eventId;
        Status = Enum.GetName(typeof(BookingStatus), status)?.ToLower();
        ProcessedAt = processedAt;
    }
}