using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Booking.Application.Interfaces;

public interface IBookingProducer
{
    Task PublishBookingConfirmedMessageAsync(BookingConfirmed message, CancellationToken ct = default);
    Task PublishBookingRejectedMessageAsync(BookingRejected message, CancellationToken ct = default);
}
