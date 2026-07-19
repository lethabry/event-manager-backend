using EventManager.Models;

namespace EventManager.Data.EventRepository;

public interface IEventRepository
{
    Task<IReadOnlyList<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<Event?> CreateEventAsync(CreateEventDTO newEvent);
    Task<Event?> UpdateEventAsync(Guid id, EventInfoDTO updatedEvent);
    Task<bool> DeleteEventAsync(Guid id);
}
