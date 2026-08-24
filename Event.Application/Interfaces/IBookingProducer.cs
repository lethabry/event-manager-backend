using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Event.Application.Interfaces;

public interface IBookingProducer
{
    Task PublishBookingRejectedMessageAsync(BookingRejected message, CancellationToken ct = default);
}
