using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Event.Application.Services.BookingMessagesProcess;

public interface IBookingMessagesProcess
{
    public Task HandleProcessAsync(BookingConfirmed message, CancellationToken cancellationToken);
    public Task HandleProcessAsync(BookingCancelled message, CancellationToken cancellationToken);
}
