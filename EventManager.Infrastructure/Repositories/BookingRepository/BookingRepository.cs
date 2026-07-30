using EventManager.Application.Interfaces;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManager.Infrastructure.Repositories.BookingRepository;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(AppDbContext appDbContext)
        : this(appDbContext, NullLogger<BookingRepository>.Instance)
    {
    }

    public BookingRepository(AppDbContext appDbContext, ILogger<BookingRepository> logger)
    {
        _appDbContext = appDbContext;
        _logger = logger;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        return await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        await using var transaction = await _appDbContext.Database.BeginTransactionAsync();

        var exist = await _appDbContext.Events.AnyAsync(e => e.Id == eventId);

        if (!exist)
        {
            throw new EventNotFoundException(eventId);
        }

        var affected = await _appDbContext.Events
            .Where(e => e.Id == eventId && e.AvailableSeats > 0)
            .ExecuteUpdateAsync(update => update.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats - 1));

        if (affected == 0)
        {
            throw new NoAvailableSeatsException(eventId);
        }

        var booking = new Booking(eventId);
        await _appDbContext.Bookings.AddAsync(booking);
        await _appDbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync(BookingStatus? status = null)
    {
        var result = _appDbContext.Bookings.AsQueryable();
        if (status != null)
        {
            result = result.Where((b) => b.Status == status);
        }

        var list = await result.ToListAsync();
        return list.AsReadOnly();
    }

    public async Task<Booking?> UpdateBookingAsync(Booking updatedBooking, CancellationToken ct = default)
    {
        var booking = await GetBookingByIdAsync(updatedBooking.Id);
        if (booking == null)
        {
            throw new BookingNotFoundException(updatedBooking.Id);
        }
        _logger.LogDebug("Updating booking {BookingId}", updatedBooking.Id);
        _appDbContext.Update(updatedBooking);
        await _appDbContext.SaveChangesAsync(ct);
        return updatedBooking;
    }
}
