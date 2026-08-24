using System.Text.Json;
using Event.Application.Interfaces;
using Event.Infrastructure.Configurations;
using Confluent.Kafka;
using EventManager.Contracts.Kafka.KafkaTopics;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.Options;
namespace Event.Infrastructure.Kafka.BookingProducer;

public class BookingProducer : IBookingProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public BookingProducer(IOptions<KafkaConfiguration> kafkaConfiguration)
    {
        var configuration = kafkaConfiguration.Value;
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = configuration.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishBookingRejectedMessageAsync(BookingRejected message, CancellationToken ct = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = JsonSerializer.Serialize(message)
        };

        await _producer.ProduceAsync(KafkaTopics.BookingRejected, kafkaMessage, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
