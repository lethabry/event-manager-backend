using Booking.Domain.Common;
using BookingEntity = Booking.Domain.Models.Booking;
namespace Booking.Application.Interfaces;

public interface IBookingRepository
{
    public Task<BookingEntity?> GetBookingByIdAsync(Guid id);
    public Task<BookingEntity> CreateBookingAsync(Guid eventId, Guid userId);
    public Task<bool> CancelBookingAsync(Guid bookingId);
    public Task<IReadOnlyList<BookingEntity>> GetBookingsAsync(BookingStatus? status = null);
    public Task<BookingEntity?> UpdateBookingAsync(BookingEntity updatedBooking, CancellationToken cancellationToken = default);
    public Task<int> GetCountOfActiveBookingsAsync(Guid userId);
}
