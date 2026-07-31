using EventManager.Application.DTOs;
namespace EventManager.Application.Services.BookingService;

public interface IBookingService
{
    public Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId);
    public Task<BookingDTO?> CreateBookingAsync(Guid eventId);
}
