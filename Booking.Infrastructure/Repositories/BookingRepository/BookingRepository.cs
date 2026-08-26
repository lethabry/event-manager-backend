using Booking.Application.Interfaces;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using BookingEntity = Booking.Domain.Models.Booking;
using Booking.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Booking.Infrastructure.Repositories.BookingRepository;

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

    public async Task<BookingEntity?> GetBookingByIdAsync(Guid id)
    {
        return await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<BookingEntity> CreateBookingAsync(Guid eventId, Guid userId)
    {
        var booking = new BookingEntity(eventId, userId);
        await _appDbContext.Bookings.AddAsync(booking);
        await _appDbContext.SaveChangesAsync();
        return booking;
    }

    public async Task<IReadOnlyList<BookingEntity>> GetBookingsAsync(BookingStatus? status = null)
    {
        var result = _appDbContext.Bookings.AsQueryable();
        if (status != null)
        {
            result = result.Where((b) => b.Status == status);
        }

        var list = await result.ToListAsync();
        return list.AsReadOnly();
    }

    public async Task<BookingEntity?> UpdateBookingAsync(BookingEntity updatedBooking, CancellationToken ct = default)
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

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingNotFoundException(bookingId);
        }

        _logger.LogDebug("Cancelling booking {BookingId}", bookingId);

        if (!booking.Cancel())
        {
            throw new BookingStatusConflictException(booking.Id);
        }
        _appDbContext.Bookings.Update(booking);
        await _appDbContext.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountOfActiveBookingsAsync(Guid userId)
    {
        return await _appDbContext.Bookings.CountAsync((b) => b.UserId == userId
                                                              && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending));
    }
}
