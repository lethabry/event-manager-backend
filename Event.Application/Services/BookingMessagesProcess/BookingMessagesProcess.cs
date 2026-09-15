using Event.Application.Configurations;
using Event.Application.Interfaces;
using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Event.Application.Services.BookingMessagesProcess;

public class BookingMessagesProcess : IBookingMessagesProcess
{
    private readonly ILogger<BookingMessagesProcess> _logger;
    private readonly IEventRepository _eventRepository;
    private readonly IBookingProducer _bookingProducer;
    private readonly ICacher? _cacheRepository;
    private readonly EventCacheOptions _cacheOptions;

    public BookingMessagesProcess(ILogger<BookingMessagesProcess> logger, IEventRepository eventRepository,
        IBookingProducer bookingProducer, ICacher? cacheRepository = null, IOptions<EventCacheOptions>? cacheOptions = null)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _bookingProducer = bookingProducer;
        _cacheRepository = cacheRepository;
        _cacheOptions = cacheOptions?.Value ?? new EventCacheOptions();
    }

    public async Task HandleProcessAsync(BookingConfirmed message, CancellationToken cancellationToken)
    {
        if (IsInvalidMessage(message))
        {
            _logger.LogInformation("Отсутствуют важные компоненты сообщения");
            return;
        }

        var evt = await _eventRepository.GetEventByIdAsync(message.EventId);
        if (evt is null)
        {
            _logger.LogWarning("Мероприятие с id {EventId} не существует в базе данных", message.EventId);
            await SendRejectedMessage(message, cancellationToken);
            return;
        }
        if (evt.AvailableSeats <= 0)
        {
            _logger.LogWarning("Нет доступных мест для бронирования мероприятия с id {EventId}", message.EventId);
            await SendRejectedMessage(message, cancellationToken);
            return;
        }

        var updated = await _eventRepository.TryReserveSeatsAsync(message.EventId, message.AmountSeats);
        if (updated)
        {
            await InvalidateEventCacheAsync(message.EventId);
            _logger.LogInformation("Количество мест у мероприятия с id {EventId} уменьшено на {AmountSeats}", message.EventId, message.AmountSeats);
        }
        else
        {
            _logger.LogInformation("Не удалось уменьшить места для мероприятия c id {EventId}", message.EventId);
            await SendRejectedMessage(message, cancellationToken);
        }
    }

    public async Task HandleProcessAsync(BookingCancelled message, CancellationToken cancellationToken)
    {
        if (IsInvalidMessage(message))
        {
            _logger.LogInformation("Отсутствуют важные компоненты сообщения");
            return;
        }

        var evt = await _eventRepository.GetEventByIdAsync(message.EventId);
        if (evt is null)
        {
            _logger.LogWarning("Мероприятие с id {EventId} не существует в базе данных", message.EventId);
            return;
        }

        var updated = await _eventRepository.TryReleaseSeatsAsync(message.EventId, message.AmountSeats);
        if (updated)
        {
            await InvalidateEventCacheAsync(message.EventId);
            _logger.LogInformation("Количество мест у мероприятия с id {EventId} увеличено на {AmountSeats}", message.EventId, message.AmountSeats);
        }
        else
        {
            _logger.LogInformation("Не удалось увеличить места для мероприятия c id {EventId}", message.EventId);
        }
    }

    private bool IsInvalidMessage(BookingConfirmed message)
    {
        return message.AmountSeats <= 0 || message.EventId == Guid.Empty || message.UserId == Guid.Empty || message.BookingId == Guid.Empty;
    }

    private bool IsInvalidMessage(BookingCancelled message)
    {
        return message.AmountSeats <= 0 || message.EventId == Guid.Empty || message.UserId == Guid.Empty || message.BookingId == Guid.Empty;
    }

    private async Task InvalidateEventCacheAsync(Guid eventId)
    {
        if (_cacheRepository == null)
        {
            return;
        }

        await _cacheRepository.TryDeleteDataAsync($"{_cacheOptions.EventKeyPrefix}:{eventId}");
    }

    private async Task SendRejectedMessage(BookingConfirmed message, CancellationToken cancellationToken)
    {
        var bookingRejectedMessage = new BookingRejected()
        {
            EventId = message.EventId,
            BookingId = message.BookingId,
            UserId = message.UserId,
            AmountSeats = message.AmountSeats,
        };
        await _bookingProducer.PublishBookingRejectedMessageAsync(bookingRejectedMessage, cancellationToken);
    }
}
