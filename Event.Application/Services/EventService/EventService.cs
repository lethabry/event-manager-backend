using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Application.Services.EventValidatorService;
using Event.Domain.Exceptions;
using Event.Domain.Models;
using EventEntity = Event.Domain.Models.Event;

namespace Event.Application.Services.EventService;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IEventValidatorService _validation;

    public EventService(IEventRepository repository, IEventValidatorService validation)
    {
        _repository = repository;
        _validation = validation;
    }

    public async Task<PaginatedResultDTO<EventEntity>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        _validation.ValidatePaginatedResult(from, to, page, pageSize);
        var events = await _repository.GetEventsAsync(title, from, to, page, pageSize);
        var totalAmount = await _repository.GetEventsCountAsync(title, from, to);
        var result = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = events,
            TotalAmount = totalAmount
        };
        return result;
    }

    public async Task<EventEntity?> GetEventByIdAsync(Guid id)
    {
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventNotFoundException(id);
        }

        return existing;
    }

    public async Task<EventEntity?> CreateEventAsync(CreateEventDTO newEvent)
    {
        _validation.ValidateEventDTO(newEvent);
        return await _repository.CreateEventAsync(newEvent);
    }

    public async Task<EventEntity?> UpdateEventAsync(Guid id, EventInfoDTO updatedEvent)
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
