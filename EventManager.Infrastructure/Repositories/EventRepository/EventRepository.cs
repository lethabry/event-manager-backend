using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManager.Infrastructure.Repositories.EventRepository;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(AppDbContext appDbContext)
        : this(appDbContext, NullLogger<EventRepository>.Instance)
    {
    }

    public EventRepository(AppDbContext appDbContext, ILogger<EventRepository> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Event>> GetEventsAsync(string? title, DateTime? from, DateTime? to)
    {
        var result = _appDbContext.Events.AsQueryable();
        if (!string.IsNullOrEmpty(title))
        {
            result = result.Where((evt) => evt.Title.ToLower().Contains(title.ToLower()));
        }

        if (from.HasValue)
        {
            result = result.Where((evt) => evt.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            result = result.Where((evt) => evt.EndAt <= to.Value);
        }

        var list = await result.Include(e => e.Bookings).ToListAsync();
        return list.AsReadOnly();
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _appDbContext.Events.Select(e => e).Where(e => e.Id == id).Include(e => e.Bookings).FirstOrDefaultAsync();
    }

    public async Task<Event?> CreateEventAsync(CreateEventDTO newEvent)
    {
        var createdEvent = Event.Create(newEvent.Title, newEvent.StartAt, newEvent.EndAt, newEvent.TotalSeats, string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description);
        _logger.LogDebug("Creating event {EventId}", createdEvent.Id);
        _appDbContext.Events.Add(createdEvent);
        await _appDbContext.SaveChangesAsync();
        return createdEvent;
    }

    public async Task<Event?> UpdateEventAsync(Guid id, EventInfoDTO eventDto)
    {
        var evt = _appDbContext.Events.Include(e => e.Bookings).FirstOrDefault(e => e.Id == id);
        if (evt == null)
        {
            return null;
        }
        evt.Title = eventDto.Title;
        evt.StartAt = eventDto.StartAt;
        evt.EndAt = eventDto.EndAt;
        evt.TotalSeats = eventDto.TotalSeats;
        evt.AvailableSeats = eventDto.AvailableSeats;
        evt.Description = eventDto.Description;
        _logger.LogDebug("Updating event {EventId}", id);
        await _appDbContext.SaveChangesAsync();
        return evt;
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        var evt = _appDbContext.Events.FirstOrDefault(e => e.Id == id);
        if (evt == null)
        {
            return false;
        }
        _logger.LogDebug("Deleting event {EventId}", id);
        _appDbContext.Events.Remove(evt);
        await _appDbContext.SaveChangesAsync();
        return true;
    }
}