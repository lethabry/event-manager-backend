using Event.Application.DTOs;
using EventEntity = Event.Domain.Models.Event;
using Event.Domain.Models;

namespace Event.Application.Services.EventService;

public interface IEventService
{
    Task<PaginatedResultDTO<EventEntity>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<EventEntity?> GetEventByIdAsync(Guid id);
    Task<EventEntity?> CreateEventAsync(CreateEventDTO newEvent);
    Task<EventEntity?> UpdateEventAsync(Guid id, EventInfoDTO updatedEvent);
    Task DeleteEventAsync(Guid id);
    Task <IReadOnlyList<EventEntity>> GetTopEventsAsync();
}
