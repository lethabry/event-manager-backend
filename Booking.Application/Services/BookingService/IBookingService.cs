using Booking.Application.DTOs;
using EventManager.Contracts.Common;
namespace Booking.Application.Services.BookingService;

public interface IBookingService
{
    public Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId);
    public Task<BookingDTO?> CreateBookingAsync(Guid eventId, Guid userId);
    public Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role);
}
