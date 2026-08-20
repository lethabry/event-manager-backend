using Event.Application.DTOs;
using EventEntity = Event.Domain.Models.Event;
namespace Event.Application.Interfaces;

public interface IEventRepository
{
    Task<IReadOnlyList<EventEntity>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> GetEventsCountAsync(string? title, DateTime? from, DateTime? to);
    Task<EventEntity?> GetEventByIdAsync(Guid id);
    Task<EventEntity?> CreateEventAsync(CreateEventDTO newEvent);
    Task<EventEntity?> UpdateEventAsync(EventEntity updatedEvent);
    Task<bool> DeleteEventAsync(Guid id);
}
