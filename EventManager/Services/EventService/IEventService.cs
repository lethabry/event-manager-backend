using EventManager.Models;

namespace EventManager.Services.EventService;

public interface IEventService
{
    PaginatedResultDTO<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Event? GetEventById(Guid id);
    Event CreateEvent(CreateEventDTO newEvent);
    Event UpdateEvent(Guid id, EventInfoDTO updatedEvent);
    void DeleteEvent(Guid id);
}