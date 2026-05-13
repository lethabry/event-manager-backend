using EventManager.Models;

namespace EventManager.Data;

public interface IEventRepository
{
    IReadOnlyCollection<Event> GetEvents(string? title, DateTime? from, DateTime? to);
    Event? GetEventById(Guid id);
    Event CreateEvent(EventDTO newEvent);
    Event? UpdateEvent(Guid id, EventDTO updatedEvent);
    bool DeleteEvent(Guid id);
}