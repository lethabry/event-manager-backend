using EventManager.Common;
using EventManager.Models;

namespace EventManager.Data.BookingRepository;

public interface IBookingRepository
{
    public Task<Booking?> GetBookingByIdAsync(Guid id);
    public Booking? CreateBookingAsync(Guid eventId);
    public Task<IReadOnlyList<Booking>> GetBookings(BookingStatus? status = null);
    public Task<Booking?> UpdateBooking(Booking updatedBooking, CancellationToken cancellationToken = default);
}