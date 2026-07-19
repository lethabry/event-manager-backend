using EventManager.Data.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Data.EventRepository;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _appDbContext;

    public EventRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
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

        return result.ToList().AsReadOnly();
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        return await _appDbContext.Events.Select(e => e).Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Event?> CreateEventAsync(CreateEventDTO newEvent)
    {
        try
        {
            var createdEvent = Event.Create(newEvent.Title, newEvent.StartAt, newEvent.EndAt, newEvent.TotalSeats, string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description);
            _appDbContext.Events.Add(createdEvent);
            await _appDbContext.SaveChangesAsync();
            return createdEvent;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occured during creating event: Exception {e}");
            throw;
        }
    }

    public async Task<Event?> UpdateEventAsync(Guid id, EventInfoDTO eventDto)
    {
        var evt = _appDbContext.Events.FirstOrDefault(e => e.Id == id);
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
        _appDbContext.Events.Remove(evt);
        await _appDbContext.SaveChangesAsync();
        return true;
    }
}
