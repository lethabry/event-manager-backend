using Event.Application.Configurations;
using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Application.Services.EventValidatorService;
using Event.Domain.Exceptions;
using Event.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventEntity = Event.Domain.Models.Event;

namespace Event.Application.Services.EventService;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;
    private readonly IEventValidatorService _validation;
    private readonly ICacher _cacheRepository;
    private readonly ILogger<EventService> _logger;
    private readonly EventCacheOptions _cacheOptions;

    public EventService(
        IEventRepository repository,
        IEventValidatorService validation,
        ICacher cacher,
        ILogger<EventService> logger,
        IOptions<EventCacheOptions> cacheOptions
    )
    {
        _repository = repository;
        _validation = validation;
        _cacheRepository = cacher;
        _logger = logger;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<PaginatedResultDTO<EventEntity>> GetEventsAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize
    )
    {
        _validation.ValidatePaginatedResult(from, to, page, pageSize);
        var events = await _repository.GetEventsAsync(title, from, to, page, pageSize);
        var totalAmount = await _repository.GetEventsCountAsync(title, from, to);
        var result = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = events,
            TotalAmount = totalAmount,
        };
        return result;
    }

    public async Task<EventEntity?> GetEventByIdAsync(Guid id)
    {
        var cacheKey = GetEventCacheKey(id);
        var cachedData = await _cacheRepository.GetDataByKeyAsync<CachedEventDTO>(cacheKey);

        if (cachedData != null)
        {
            return cachedData.ToEvent();
        }

        var existing = await _repository.GetEventByIdAsync(id);
        if (existing == null)
        {
            throw new EventNotFoundException(id);
        }
        var isSaveInCache = await _cacheRepository.TryWriteDataAsync(
            cacheKey,
            CachedEventDTO.FromEvent(existing),
            _cacheOptions.EventByIdTtl
        );
        if (isSaveInCache)
        {
            _logger.LogInformation("Event sent to cache successfully");
        }
        return existing;
    }

    public async Task<EventEntity?> CreateEventAsync(CreateEventDTO newEvent)
    {
        _validation.ValidateEventDTO(newEvent);
        var createdEvent = await _repository.CreateEventAsync(newEvent);
        if (createdEvent != null)
        {
            await InvalidateTopEventsCacheAsync();
        }

        return createdEvent;
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
            string.IsNullOrEmpty(updatedEvent.Description) ? null : updatedEvent.Description
        );
        var savedEvent = await _repository.UpdateEventAsync(existing);
        if (savedEvent == null)
        {
            throw new EventNotFoundException(id);
        }

        var cacheKey = GetEventCacheKey(id);
        var isCacheSaved = await _cacheRepository.TryWriteDataAsync(
            cacheKey,
            CachedEventDTO.FromEvent(savedEvent),
            _cacheOptions.EventByIdTtl
        );
        if (isCacheSaved)
        {
            _logger.LogInformation("Event saved to cache successfully");
        }

        await InvalidateTopEventsCacheAsync();

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

        var isCacheDeleted = await _cacheRepository.TryDeleteDataAsync(GetEventCacheKey(id));
        if (isCacheDeleted)
        {
            _logger.LogInformation("Event deleted from cache successfully");
        }

        await InvalidateTopEventsCacheAsync();
    }

    public async Task<IReadOnlyList<EventEntity>> GetTopEventsAsync()
    {
        var cachedTopEvents = await _cacheRepository.GetDataByKeyAsync<List<CachedEventDTO>>(
            _cacheOptions.TopEventsKey
        );
        if (cachedTopEvents != null)
        {
            return cachedTopEvents.Select(evt => evt.ToEvent()).ToList().AsReadOnly();
        }

        var topEvents = await _repository.GetTopEventsAsync();

        if (topEvents.Count > 0)
        {
            var cachedEvents = topEvents.Select(CachedEventDTO.FromEvent).ToList();
            var isCacheSaved = await _cacheRepository.TryWriteDataAsync(
                _cacheOptions.TopEventsKey,
                cachedEvents,
                _cacheOptions.TopEventsTtl
            );
            if (isCacheSaved)
            {
                _logger.LogInformation("Top events saved to cache successfully");
            }
        }
        return topEvents;
    }

    private string GetEventCacheKey(Guid id)
    {
        return $"{_cacheOptions.EventKeyPrefix}:{id}";
    }

    private async Task InvalidateTopEventsCacheAsync()
    {
        var isCacheDeleted = await _cacheRepository.TryDeleteDataAsync(_cacheOptions.TopEventsKey);
        if (isCacheDeleted)
        {
            _logger.LogInformation("Top events cache invalidated successfully");
        }
    }
}
