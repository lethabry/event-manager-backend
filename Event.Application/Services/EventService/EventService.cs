using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Application.Services.EventValidatorService;
using Event.Domain.Exceptions;
using Event.Domain.Models;
using Microsoft.Extensions.Logging;
using EventEntity = Event.Domain.Models.Event;

namespace Event.Application.Services.EventService;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IEventValidatorService _validation;
    private readonly ICacher _cacheRepository;
    private readonly ILogger<EventService> _logger;
    
    public EventService(IEventRepository repository, IEventValidatorService validation, ICacher cacher, ILogger<EventService> logger)
    {
        _repository = repository;
        _validation = validation;
        _cacheRepository = cacher;
        _logger = logger;
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
        var cachedData = await _cacheRepository.GetDataByKeyAsync<EventEntity>($"event:{id.ToString()}");
        
        if (cachedData != null)
        {
            return cachedData;
        }
        
        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventNotFoundException(id);
        }
        var isSaveInCache = await _cacheRepository.TryWriteDataAsync($"event:{id.ToString()}", existing, 10);
        if (isSaveInCache)
        {
            _logger.LogInformation("Event sent to cache successfully");
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
        
        var cachedData = await _cacheRepository.GetDataByKeyAsync<EventEntity>($"event:{id.ToString()}");
        if (cachedData != null)
        {
            var isCacheSaved = await _cacheRepository.TryWriteDataAsync($"event:{id.ToString()}", savedEvent, 30);
            if (isCacheSaved)
            {
                _logger.LogInformation("Event saved to cache successfully");
            }
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
        
        var isCacheDeleted = await _cacheRepository.TryDeleteDataAsync($"event:{id.ToString()}");
        if (isCacheDeleted)
        {
            _logger.LogInformation("Event deleted from cache successfully");
        }
    }
    
    public async Task<IReadOnlyList<EventEntity>> GetTopEventsAsync()
    {
        var cachedTopEvents = await _cacheRepository.GetDataByKeyAsync<IReadOnlyList<EventEntity>>("events:top10");
        if (cachedTopEvents != null)
        {
            return cachedTopEvents;
        }
        
        var topEvents = await _repository.GetTopEventsAsync();
        
        if (topEvents.Count > 0)
        {
            var isCacheSaved = await _cacheRepository.TryWriteDataAsync("events:top10", topEvents, 30);
            if (isCacheSaved)
            {
                _logger.LogInformation("Top events saved to cache successfully");
            }
        }
        return topEvents;
    }
}
