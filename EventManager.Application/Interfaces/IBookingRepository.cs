using EventManager.Domain.Common;
using EventManager.Domain.Models;
namespace EventManager.Application.Interfaces;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Task<Booking?> CreateBookingAsync(Guid eventId);
    public Task<IReadOnlyList<Booking>> GetBookingsAsync(BookingStatus? status = null);
    public Task<Booking?> UpdateBookingAsync(Booking updatedBooking, CancellationToken cancellationToken = default);
}
