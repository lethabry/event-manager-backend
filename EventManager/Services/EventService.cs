using EventManager.Models;
using EventManager.Services.Interfaces;

namespace EventManager.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public List<Event> GetEvents()
    {
        return _repository.GetEvents();
    }

    public Event GetEventById(Guid id)
    {
        var existing = _repository.GetEventById(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public Event CreateEvent(EventDTO newEvent)
    {
        if (string.IsNullOrWhiteSpace(newEvent.Title))
        {
            throw new ArgumentException("Название мероприятия не может быть пустым");
        }

        if (newEvent.StartAt >= newEvent.EndAt)
        {
            throw new ArgumentException(
                "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }

        return _repository.CreateEvent(newEvent);
    }

    public Event UpdateEvent(Guid id, EventDTO updatedEvent)
    {
        if (string.IsNullOrWhiteSpace(updatedEvent.Title))
        {
            throw new ArgumentException("Название мероприятия не может быть пустым");
        }

        if (updatedEvent.StartAt >= updatedEvent.EndAt)
        {
            throw new ArgumentException(
                "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }

        var existing = _repository.UpdateEvent(id, updatedEvent);

        if (existing == null)
        {
            throw new KeyNotFoundException($"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public void DeleteEvent(Guid id)
    {
        var existing = _repository.GetEventById(id);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Мероприятие с id {id} не найдено");
        }

        _repository.DeleteEvent(id);
    }
}