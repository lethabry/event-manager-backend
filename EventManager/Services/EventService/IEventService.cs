using EventManager.Models;

namespace EventManager.Services.EventService;

public interface IEventService
{
    IReadOnlyCollection<Event> GetEvents();
    Event? GetEventById(Guid id);
    Event CreateEvent(EventDTO newEvent);
    Event UpdateEvent(Guid id, EventDTO updatedEvent);
    void DeleteEvent(Guid id);
}