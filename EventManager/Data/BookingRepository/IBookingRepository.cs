using EventManager.Common;
using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Task<Booking?> CreateBookingAsync(Guid eventId);
    public Task<IReadOnlyList<Booking>> GetBookingsAsync(BookingStatus? status = null);
    public Task<Booking?> UpdateBookingAsync(Booking updatedBooking, CancellationToken cancellationToken = default);
}