using System.Net;
using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.ValidationService;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Models;

namespace EventManager.Application.Services.EventService;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IValidationService _validation;

    public EventService(IEventRepository repository, IValidationService validation)
    {
        _repository = repository;
        _validation = validation;
    }

    public async Task<PaginatedResultDTO<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        _validation.ValidatePaginatedResult(from, to, page, pageSize);
        var events = await _repository.GetEventsAsync(title, from, to);
        var paginatedEvents = events.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var result = new PaginatedResultDTO<Event>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = paginatedEvents,
            TotalAmount = events.Count
        };
        return result;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public async Task<Event?> CreateEventAsync(CreateEventDTO newEvent)
    {
        _validation.ValidateEventDTO(newEvent);
        return await _repository.CreateEventAsync(newEvent);
    }

    public async Task<Event?> UpdateEventAsync(Guid id, EventInfoDTO updatedEvent)
    {
        _validation.ValidateEventDTO(updatedEvent);
        var existing = await _repository.UpdateEventAsync(id, updatedEvent);

        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }

        return existing;
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено");
        }
        var isDeleted = await _repository.DeleteEventAsync(id);
        if (!isDeleted)
        {
            throw new EventException(HttpStatusCode.InternalServerError, "Не удалось удалить мероприятие");
        }
    }
}
