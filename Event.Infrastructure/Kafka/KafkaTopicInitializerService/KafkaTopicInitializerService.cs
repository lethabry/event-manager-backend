using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Event.Infrastructure.Configurations;
using EventManager.Contracts.Kafka.KafkaTopics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Event.Infrastructure.Kafka.KafkaTopicInitializerService;

public sealed class KafkaTopicInitializerService : IHostedService
{
    private readonly KafkaConfiguration _configuration;
    private readonly ILogger<KafkaTopicInitializerService> _logger;

    public KafkaTopicInitializerService(IOptions<KafkaConfiguration> configuration, ILogger<KafkaTopicInitializerService> logger)
    {
        _configuration = configuration.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return EnsureTopicsCreatedAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureTopicsCreatedAsync()
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = _configuration.BootstrapServers
                })
                .Build();

            List<TopicSpecification> topics =
            [
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingEvents,
                    NumPartitions = _configuration.TopicPartitions,
                    ReplicationFactor = _configuration.TopicReplicationFactor
                },
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingRejected,
                    NumPartitions = _configuration.TopicPartitions,
                    ReplicationFactor = _configuration.TopicReplicationFactor
                },
            ];
            await adminClient.CreateTopicsAsync(topics);

            _logger.LogInformation("Kafka топики созданы");
        }
        catch (CreateTopicsException exception)
        {
            var errors = exception.Results
                .Where(result => result.Error.Code != ErrorCode.TopicAlreadyExists)
                .ToArray();

            foreach (var error in errors)
            {
                _logger.LogWarning("Ошибка при создании топика {Topic}: {Reason}", error.Topic, error.Error.Reason);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Kafka topic initialization failed");
        }
    }
}
