using Booking.Application.Interfaces;
using Booking.Domain.Common;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.Logging;

namespace Booking.Application.Services.BookingMessagesProcess;

public class BookingMessagesProcess : IBookingMessagesProcess
{
    private readonly ILogger<BookingMessagesProcess> _logger;
    private readonly IBookingRepository _bookingRepository;

    public BookingMessagesProcess(ILogger<BookingMessagesProcess> logger, IBookingRepository bookingRepository)
    {
        _logger = logger;
        _bookingRepository = bookingRepository;
    }

    public async Task HandleProcessAsync(BookingRejected message, CancellationToken cancellationToken)
    {
        if (IsInvalidMessage(message))
        {
            _logger.LogInformation("Отсутствуют важные компоненты сообщения");
            return;
        }

        var booking = await _bookingRepository.GetBookingByIdAsync(message.BookingId);
        if (booking is null)
        {
            _logger.LogInformation("Бронирование с id {BookingId} не найдено", message.BookingId);
            return;
        }
        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Rejected)
        {
            _logger.LogInformation("Бронирование с id {BookingId} уже отменено", message.BookingId);
            return;
        }
        booking.Reject();
        await _bookingRepository.UpdateBookingAsync(booking, cancellationToken);
    }

    private bool IsInvalidMessage(BookingRejected message)
    {
        return message.AmountSeats <= 0 || message.EventId == Guid.Empty || message.UserId == Guid.Empty || message.BookingId == Guid.Empty;
    }
}
