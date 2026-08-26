using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Booking.Application.Interfaces;

public interface IBookingProducer
{
    Task PublishBookingConfirmedMessageAsync(BookingConfirmed message, CancellationToken ct = default);
    Task PublishBookingCancelledMessageAsync(BookingCancelled message, CancellationToken ct = default);
}
