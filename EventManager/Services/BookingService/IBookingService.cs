using EventManager.Models;

namespace EventManager.Services.BookingService;

public interface IBookingService
{
    public Task<Booking?> GetBookingByIdAsync(Guid bookingId);
    public Task<Booking?> CreateBookingAsync(Guid eventId);
}