using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Task<Booking?> CreateBookingAsync(Guid eventId);
    public List<Booking> GetPendingBookings(out List<Booking> bookings);
}