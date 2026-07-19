using EventManager.Models;

namespace EventManager.Services.EventService;

public interface IEventService
{
    Task<PaginatedResultDTO<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<Event?> CreateEventAsync(CreateEventDTO newEvent);
    Task<Event?> UpdateEventAsync(Guid id, EventInfoDTO updatedEvent);
    Task DeleteEventAsync(Guid id);
}
