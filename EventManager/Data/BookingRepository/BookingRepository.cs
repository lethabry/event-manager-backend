using System.Collections.Concurrent;
using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public class BookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings;

    public BookingRepository()
    {
        _bookings = new ConcurrentDictionary<Guid, Booking>();
    }

    public Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        Task.Delay(1000);
        return Task.Run(() => _bookings.TryGetValue(id, out var booking) ? booking : null);
    }

    public Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        Task.Delay(1000);
        var booking = new Booking(eventId);
        return Task.Run(() => _bookings.TryAdd(eventId, booking) ? booking : null);
    }
}