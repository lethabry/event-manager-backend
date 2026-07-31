using EventManager.Application.DTOs;
using EventManager.Domain.Models;
namespace EventManager.Application.Interfaces;

public interface IEventRepository
{
    Task<IReadOnlyList<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> GetEventsCountAsync(string? title, DateTime? from, DateTime? to);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<Event?> CreateEventAsync(CreateEventDTO newEvent);
    Task<Event?> UpdateEventAsync(Event updatedEvent);
    Task<bool> DeleteEventAsync(Guid id);
}
