using System.Net;
using EventManager.Application.Interfaces;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Repositories.BookingRepository;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _appDbContext;

    public BookingRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        return await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        try
        {
            var booking = new Booking(eventId);
            _appDbContext.Bookings.Add(booking);
            await _appDbContext.SaveChangesAsync();
            return booking;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error occured during creating booking: Exception {e}");
            throw;
        }
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
            throw new BookingException(HttpStatusCode.NotFound, $"Бронирование с id {updatedBooking.Id} не найдено");
        }
        _appDbContext.Update(updatedBooking);
        await _appDbContext.SaveChangesAsync(ct);
        return updatedBooking;
    }
}
