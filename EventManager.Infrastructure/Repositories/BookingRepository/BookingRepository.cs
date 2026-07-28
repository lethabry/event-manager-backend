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

    public async Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        var booking = new Booking(eventId);
        _logger.LogDebug("Creating booking {BookingId} for event {EventId}", booking.Id, eventId);
        _appDbContext.Bookings.Add(booking);
        await _appDbContext.SaveChangesAsync();
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
            throw new BookingException(404, $"Бронирование с id {updatedBooking.Id} не найдено", updatedBooking.Id);
        }
        _logger.LogDebug("Updating booking {BookingId}", updatedBooking.Id);
        _appDbContext.Update(updatedBooking);
        await _appDbContext.SaveChangesAsync(ct);
        return updatedBooking;
    }
}