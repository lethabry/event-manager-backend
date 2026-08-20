namespace Booking.Application.Services.BookingConfirmationService;

public interface IBookingConfirmationService
{
    Task ProcessPendingBookingsAsync(CancellationToken ct);
    Task ProcessBookingAsync(Guid bookingId, CancellationToken ct);
}