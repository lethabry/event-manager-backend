using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.EventService;

namespace EventManager.BackgroundServices;

/// <summary>
/// Сервис для имитации запроса на внешний сервис для подтверждение бронирования
/// </summary>
public class BookingConfirmationService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingConfirmationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingConfirmationService(IBookingRepository bookingRepository, ILogger<BookingConfirmationService> logger, IServiceScopeFactory scopeFactory)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
        _scopeFactory = scopeFactory;
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

            await Task.Delay(AppConstants.DelayBetweenBookingConfirmationInteration, stoppingToken);
        }

        _logger.LogInformation("BookingConfirmationService остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(AppConstants.DelayBetweenBookingConfirmationHandling, stoppingToken);
        Event? evt = null;
        using var scope = _scopeFactory.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        await _processingSemaphore.WaitAsync(stoppingToken);
        try
        {
            evt = eventService.GetEventById(booking.EventId);
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
            await _bookingRepository.UpdateBooking(booking, stoppingToken);
            if (evt != null)
            {
                evt.ReleaseSeats();
                var updatedEvt = new EventInfoDTO()
                {
                    Title = evt.Title,
                    Description = string.IsNullOrEmpty(evt.Description) ? null : evt.Description,
                    StartAt = evt.StartAt,
                    EndAt = evt.EndAt,
                    TotalSeats = evt.TotalSeats,
                    AvailableSeats = evt.AvailableSeats
                };
                eventService.UpdateEvent(evt.Id, updatedEvt);
            }
            _logger.LogError(e.Message, e);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}
