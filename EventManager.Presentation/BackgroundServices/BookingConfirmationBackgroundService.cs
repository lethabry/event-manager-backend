using EventManager.Application.Services.BookingConfirmationService;
using EventManager.Domain.Common;

namespace EventManager.Presentation.BackgroundServices;

/// <summary>
/// Сервис для имитации запроса на внешний сервис для подтверждения бронирования
/// </summary>
public class BookingConfirmationBackgroundService : BackgroundService
{
    private readonly ILogger<BookingConfirmationBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BookingConfirmationBackgroundService(ILogger<BookingConfirmationBackgroundService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmationBackgroundService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingConfirmationService = scope.ServiceProvider.GetRequiredService<IBookingConfirmationService>();
                await bookingConfirmationService.ProcessPendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Сервис заканчивает работу");
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Ошибка при подтверждении бронирований {e.Message}");
            }

            await Task.Delay(AppConstants.DelayBetweenBookingConfirmationInteration, stoppingToken);
        }

        _logger.LogInformation("BookingConfirmationBackgroundService остановлен");
    }
}
