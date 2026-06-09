using EventManager.Models;

namespace EventManager.Data.EventRepository;

public interface IEventRepository
{
    IReadOnlyCollection<Event> GetEvents(string? title, DateTime? from, DateTime? to);
    Event? GetEventById(Guid id);
    Event? CreateEvent(CreateEventDTO newEvent);
    Event? UpdateEvent(Guid id, EventInfoDTO updatedEvent);
    bool DeleteEvent(Guid id);
}
