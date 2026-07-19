using System.Net;
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
    private readonly ILogger<BookingConfirmationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingConfirmationService(ILogger<BookingConfirmationService> logger, IServiceScopeFactory scopeFactory)
    {
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
                using var scope = _scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var pendingBookings = await bookingRepository.GetBookingsAsync(BookingStatus.Pending);
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking.Id, stoppingToken));
                _logger.LogInformation("tasks length {0}", tasks.Count());
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Сервис заканчивает работу");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка при обновлении статуса бронирования {e.Message}");
            }

            await Task.Delay(AppConstants.DelayBetweenBookingConfirmationInteration, stoppingToken);
        }

        _logger.LogInformation("BookingConfirmationService остановлен");
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        await Task.Delay(AppConstants.DelayBetweenBookingConfirmationHandling, stoppingToken);
        Event? evt = null;
        using var scope = _scopeFactory.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var booking = await bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.NotFound, $"Не найдено броинрование с id {bookingId}");
        }
        try
        {
            evt = await eventService.GetEventByIdAsync(booking.EventId);

            if (evt == null)
            {
                booking.Reject();
                _logger.LogWarning($"Мероприятия с id {booking.EventId} не найдено");
            }
            else
            {
                booking.Confirm();
            }
            await bookingRepository.UpdateBookingAsync(booking, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Сервис останавливает работу");
        }
        catch (Exception e)
        {
            booking.Reject();
            await bookingRepository.UpdateBookingAsync(booking, stoppingToken);
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
                await eventService.UpdateEventAsync(evt.Id, updatedEvt);
            }
            _logger.LogError(e.Message, e);
        }
    }
}
