using EventEntity = Event.Domain.Models.Event;

namespace Event.Application.DTOs;

public sealed class CachedEventDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }

    public static CachedEventDTO FromEvent(EventEntity evt)
    {
        return new CachedEventDTO
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            StartAt = evt.StartAt,
            EndAt = evt.EndAt,
            TotalSeats = evt.TotalSeats,
            AvailableSeats = evt.AvailableSeats
        };
    }

    public EventEntity ToEvent()
    {
        return EventEntity.Reconstruct(Id, Title, StartAt, EndAt, TotalSeats, AvailableSeats, Description);
    }
}
