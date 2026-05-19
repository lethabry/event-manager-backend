using System.Collections.Concurrent;
using EventManager.Common;
using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public class BookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings;

    public BookingRepository()
    {
        _bookings = new ConcurrentDictionary<Guid, Booking>();
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        await Task.Delay(1000);
        return _bookings.TryGetValue(id, out var booking) ? booking : null;
    }

    public async Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        await Task.Delay(1000);
        var booking = new Booking(eventId);
        return _bookings.TryAdd(booking.Id, booking) ? booking : null;
    }

    public List<Booking> GetPendingBookings(out List<Booking> bookings)
    {
        var bookingsList = _bookings.Values.ToList();
        return bookings = bookingsList.Where((b) => b.Status == BookingStatus.Pending).ToList();
    }
}