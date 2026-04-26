using EventManager.Models;

namespace EventManager.Services.Interfaces;

public interface IEventService
{
    List<Event> GetEvents();
    Event? GetEventById(Guid id);
    Event CreateEvent(EventDTO newEvent);
    Event UpdateEvent(Guid id, EventDTO updatedEvent);
    bool DeleteEvent(Guid id);
}