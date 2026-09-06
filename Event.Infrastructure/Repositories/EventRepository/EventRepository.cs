using Event.Application.DTOs;
using Event.Application.Interfaces;
using EventEntity = Event.Domain.Models.Event;
using Event.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Event.Infrastructure.Repositories.EventRepository;

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
    
    public async Task<IReadOnlyList<EventEntity>> GetEventsAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var list = await FilterEvents(title, from, to)
            .OrderBy(evt => evt.StartAt)
            .ThenBy(evt => evt.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return list.AsReadOnly();
    }
    
    public Task<int> GetEventsCountAsync(string? title, DateTime? from, DateTime? to)
    {
        return FilterEvents(title, from, to).CountAsync();
    }
    
    public async Task<EventEntity?> GetEventByIdAsync(Guid id)
    {
        return await _appDbContext.Events.FirstOrDefaultAsync(evt => evt.Id == id);
    }
    
    public async Task<EventEntity?> CreateEventAsync(CreateEventDTO newEvent)
    {
        var createdEvent = EventEntity.Create(newEvent.Title, newEvent.StartAt, newEvent.EndAt, newEvent.TotalSeats, string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description);
        _logger.LogDebug("Creating event {EventId}", createdEvent.Id);
        _appDbContext.Events.Add(createdEvent);
        await _appDbContext.SaveChangesAsync();
        return createdEvent;
    }
    
    public async Task<EventEntity?> UpdateEventAsync(EventEntity updatedEvent)
    {
        var exists = await _appDbContext.Events.AnyAsync(evt => evt.Id == updatedEvent.Id);
        if (!exists)
        {
            return null;
        }
        
        if (_appDbContext.Entry(updatedEvent).State == EntityState.Detached)
        {
            _appDbContext.Events.Update(updatedEvent);
        }
        _logger.LogDebug("Updating event {EventId}", updatedEvent.Id);
        await _appDbContext.SaveChangesAsync();
        return updatedEvent;
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
    
    public async Task<bool> TryReleaseSeatsAsync(Guid id, int amountSeats)
    {
        var affected = await _appDbContext.Events
            .Where(e => e.Id == id && e.AvailableSeats + amountSeats <= e.TotalSeats)
            .ExecuteUpdateAsync(update => update.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + amountSeats));
        return affected == 1;
    }
    
    public async Task<bool> TryReserveSeatsAsync(Guid id, int amountSeats)
    {
        var affected = await _appDbContext.Events
            .Where(e => e.Id == id && e.AvailableSeats >= amountSeats)
            .ExecuteUpdateAsync(update => update.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - amountSeats));
        return affected == 1;
    }
    
    public async Task<IReadOnlyList<EventEntity>> GetTopEventsAsync()
    {
        var events = await _appDbContext.Events
            .Where(evt => evt.AvailableSeats > 0)
            .OrderByDescending(evt => (evt.TotalSeats - evt.AvailableSeats) / evt.TotalSeats)
            .Take(10)
            .ToListAsync();
        return events.AsReadOnly();
    }
    
    private IQueryable<EventEntity> FilterEvents(string? title, DateTime? from, DateTime? to)
    {
        var query = _appDbContext.Events.AsQueryable();
        if (!string.IsNullOrEmpty(title))
        {
            query = query.Where(evt => evt.Title.ToLower().Contains(title.ToLower()));
        }
        
        if (from.HasValue)
        {
            query = query.Where(evt => evt.StartAt >= from.Value);
        }
        
        if (to.HasValue)
        {
            query = query.Where(evt => evt.EndAt <= to.Value);
        }
        
        return query;
    }
}
