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

    public PaginatedResultDTO<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (from.HasValue && to.HasValue && from >= to.Value)
        {
            throw new EventException(
                HttpStatusCode.BadRequest,
                "Дата начала мероприятия должны быть раньше даты окончания мероприятия");
        }

        if (page <= 0)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Номер страницы не может быть меньше 1");
        }

        if (pageSize <= 0)
        {
            throw new EventException(HttpStatusCode.BadRequest, "Количество элементов не может быть меньше 1");
        }

        var events = _repository.GetEvents(title, from, to);
        var paginatedEvents = events.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var result = new PaginatedResultDTO<Event>()
        {
            currentPage = page,
            currentPageSize = paginatedEvents.Count,
            result = paginatedEvents,
            totalAmount = events.Count
        };
        return result;
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