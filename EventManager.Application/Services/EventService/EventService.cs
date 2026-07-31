using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.ValidationService;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;

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
        var events = await _repository.GetEventsAsync(title, from, to, page, pageSize);
        var totalAmount = await _repository.GetEventsCountAsync(title, from, to);
        var result = new PaginatedResultDTO<Event>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = events,
            TotalAmount = totalAmount
        };
        return result;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventNotFoundException(id);
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
        var existing = await _repository.GetEventByIdAsync(id);

        if (existing == null)
        {
            throw new EventNotFoundException(id);
        }

        existing.Update(
            updatedEvent.Title,
            updatedEvent.StartAt,
            updatedEvent.EndAt,
            updatedEvent.TotalSeats,
            updatedEvent.AvailableSeats,
            string.IsNullOrEmpty(updatedEvent.Description) ? null : updatedEvent.Description);
        var savedEvent = await _repository.UpdateEventAsync(existing);
        if (savedEvent == null)
        {
            throw new EventNotFoundException(id);
        }

        return savedEvent;
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventNotFoundException(id);
        }
        var isDeleted = await _repository.DeleteEventAsync(id);
        if (!isDeleted)
        {
            throw new EventDeletionFailedException();
        }
    }
}
