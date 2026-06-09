using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Models;
using EventManager.Services.EventService;

namespace EventManager.BackgroundServices;

/// <summary>
/// Сервис для имитации запроса на внешний сервис для подтверждение бронирования
/// </summary>
public class BookingConfirmationService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingConfirmationService> _logger;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingConfirmationService(IBookingRepository bookingRepository, ILogger<BookingConfirmationService> logger, IEventService eventService)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
        _eventService = eventService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmationService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = await _bookingRepository.GetBookings(BookingStatus.Pending);
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Сервис ");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка при обновлении статуса бронирования {e.Message}");
            }

            await Task.Delay(15000, stoppingToken);
        }

        _logger.LogInformation("BookingConfirmationService остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);
        _processingSemaphore.Wait(stoppingToken);
        Event? evt = null;
        try
        {
            evt = _eventService.GetEventById(booking.EventId);
            if (evt == null)
            {
                booking.Reject();
                _logger.LogWarning($"Мероприятия с id {booking.EventId} не найдено");
            }
            else
            {
                booking.Confirm();
                await _bookingRepository.UpdateBooking(booking, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Сервис останавливает работу");
        }
        catch (Exception e)
        {
            booking.Reject();
            evt?.ReleaseSeats();
            _logger.LogError(e.Message, e);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}
