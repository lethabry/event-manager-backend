using EventManager.Common;
using EventManager.Data.BookingRepository;

namespace EventManager.BackgroundServices;

/// <summary>
/// Сервис для имитации запроса на внешний сервис для подтверждение бронирования
/// </summary>
public class BookingConfirmationService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingConfirmationService> _logger;

    public BookingConfirmationService(IBookingRepository bookingRepository, ILogger<BookingConfirmationService> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmationService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _bookingRepository.GetPendingBookings(out var pendingBookings);
                foreach (var booking in pendingBookings)
                {
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(2000, stoppingToken);
                        booking.Confirm();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка при обновлении статуса бронирования {e.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }

        _logger.LogInformation("BookingConfirmationService остановлен");
    }
}