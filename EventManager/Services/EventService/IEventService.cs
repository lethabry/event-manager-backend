using EventManager.Models;

namespace EventManager.Services.EventService;

public interface IEventService
{
    PaginatedResultDTO<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Event? GetEventById(Guid id);
    Event CreateEvent(EventDTO newEvent);
    Event UpdateEvent(Guid id, EventDTO updatedEvent);
    void DeleteEvent(Guid id);
}