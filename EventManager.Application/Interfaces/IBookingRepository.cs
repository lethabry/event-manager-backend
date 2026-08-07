using EventManager.Domain.Common;
using EventManager.Domain.Models;
namespace EventManager.Application.Interfaces;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Task<Booking> CreateBookingAsync(Guid eventId, Guid userId);
    public Task<bool> CancelBookingAsync(Guid bookingId);
    public Task<IReadOnlyList<Booking>> GetBookingsAsync(BookingStatus? status = null);
    public Task<Booking?> UpdateBookingAsync(Booking updatedBooking, CancellationToken cancellationToken = default);
    public Task<int> GetCountOfActiveBookingsAsync(Guid userId);
}
