using System.Collections.Concurrent;
using System.Net;
using EventManager.Common;
using EventManager.Exceptions;
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
        return _bookings.TryGetValue(id, out var booking) ? booking : null;
    }

    public Booking? CreateBookingAsync(Guid eventId)
    {
        var booking = new Booking(eventId);
        return _bookings.TryAdd(booking.Id, booking) ? booking : null;
    }

    public async Task<IReadOnlyList<Booking>> GetBookings(BookingStatus? status = null)
    {
        IEnumerable<Booking> result = _bookings.Values;
        if (status != null)
        {
            result = result.Where((b) => b.Status == status);
        }

        return result.ToList().AsReadOnly();
    }

    public async Task<Booking?> UpdateBooking(Booking updatedBooking, CancellationToken ct = default)
    {
        var existingBooking = await GetBookingByIdAsync(updatedBooking.Id);
        if (existingBooking == null)
        {
            throw new BookingException(HttpStatusCode.NotFound, $"Бронирование с id {updatedBooking.Id} не найдено");
        }

        return _bookings.TryUpdate(updatedBooking.Id, updatedBooking, existingBooking) ? updatedBooking : null;
    }
}
