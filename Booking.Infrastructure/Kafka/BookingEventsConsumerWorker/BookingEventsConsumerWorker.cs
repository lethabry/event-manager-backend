using System.Text.Json;
using Booking.Application.Interfaces;
using Confluent.Kafka;
using Booking.Application.Services.BookingMessagesProcess;
using Booking.Domain.Common;
using Booking.Infrastructure.Configurations;
using EventManager.Contracts.Kafka.KafkaTopics;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using BookingEntity = Booking.Domain.Models.Booking;
namespace Booking.Infrastructure.Kafka.BookingEventsConsumerWorker;

public class BookingEventsConsumerWorker : BackgroundService
{
    private readonly ILogger<BookingEventsConsumerWorker> _logger;
    private readonly KafkaConfiguration _kafkaConfiguration;
    private readonly IServiceScopeFactory _scopeFactory;

    public BookingEventsConsumerWorker(ILogger<BookingEventsConsumerWorker> logger, IOptions<KafkaConfiguration> kafkaConfiguration, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _kafkaConfiguration = kafkaConfiguration.Value;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => Consume(stoppingToken), stoppingToken);
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaConfiguration.BootstrapServers,
            GroupId = _kafkaConfiguration.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _kafkaConfiguration.EnableAutoCommit,
            EnableAutoOffsetStore = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingRejected);

        _logger.LogInformation("Consumer запущен. Ожидание сообщений из топика {BookingRejected}...", KafkaTopics.BookingRejected);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult is null)
                {
                    _logger.LogInformation("Consumer сообщение невалидно");
                    return;
                }
                try
                {
                    await ProcessMessageAsync(consumeResult, stoppingToken);
                    consumer.Commit(consumeResult);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Возникла ошибка при обработке сообщений");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer остановлен штатно.");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Возникла ошибка при запуске consumer");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        var topic = result.Topic;
        switch (topic)
        {
            case KafkaTopics.BookingRejected:
            {
                var message = JsonSerializer.Deserialize<BookingRejected>(result.Message.Value);
                if (message is null)
                {
                    _logger.LogInformation("Сообщение {BookingRejected} пустое", KafkaTopics.BookingRejected);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var bookingMessagesProcess = scope.ServiceProvider.GetRequiredService<IBookingMessagesProcess>();
                await bookingMessagesProcess.HandleProcessAsync(message, ct);
                break;
            }
            default:
            {
                _logger.LogInformation("Неизвестный топик сообщения {topic}", topic);
                break;
            }
        }
    }
}
