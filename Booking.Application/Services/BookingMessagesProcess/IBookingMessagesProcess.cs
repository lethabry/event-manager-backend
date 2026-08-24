using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Booking.Application.Services.BookingMessagesProcess;

public interface IBookingMessagesProcess
{
    public Task HandleProcessAsync(BookingRejected message, CancellationToken cancellationToken);
}
