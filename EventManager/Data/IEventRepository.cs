using EventManager.Models;

namespace EventManager.Data;

public interface IEventRepository
{
    IReadOnlyCollection<Event> GetEvents();
    Event? GetEventById(Guid id);
    Event CreateEvent(EventDTO newEvent);
    Event? UpdateEvent(Guid id, EventDTO updatedEvent);
    bool DeleteEvent(Guid id);
}