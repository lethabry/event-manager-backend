using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Task<Booking?> CreateBookingAsync(Guid eventId);
}