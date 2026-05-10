using System.Net;
using EventManager.Data;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.Validation;

namespace EventManager.Services.EventService;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IValidationService _validation;

    public EventService(IEventRepository repository, IValidationService validation)
    {
        _repository = repository;
        _validation = validation;
    }

    public IReadOnlyCollection<Event> GetEvents(string? title, DateTime? from, DateTime? to)
    {
        return _repository.GetEvents(title, from, to);
    }

    public Event GetEventById(Guid id)
    {
        var existing = _repository.GetEventById(id);
        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public Event CreateEvent(EventDTO newEvent)
    {
        _validation.ValidateEventDTO(newEvent);
        return _repository.CreateEvent(newEvent);
    }

    public Event UpdateEvent(Guid id, EventDTO updatedEvent)
    {
        _validation.ValidateEventDTO(updatedEvent);
        var existing = _repository.UpdateEvent(id, updatedEvent);

        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public void DeleteEvent(Guid id)
    {
        var existing = _repository.GetEventById(id);
        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }

        _repository.DeleteEvent(id);
    }
}