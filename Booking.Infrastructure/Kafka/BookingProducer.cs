using System.Text;
using System.Text.Json;
using Booking.Application.Interfaces;
using Booking.Infrastructure.Configurations;
using Confluent.Kafka;
using EventManager.Contracts.Kafka.KafkaTopics;
using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.MessagesType;
using Microsoft.Extensions.Options;
namespace Booking.Infrastructure.Kafka;

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

    public async Task PublishBookingConfirmedMessageAsync(BookingConfirmed message, CancellationToken ct = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = JsonSerializer.Serialize(message),
            Headers = [new Header("message-type", Encoding.UTF8.GetBytes(KafkaMessageTypes.BookingConfirmed))]
        };

        await _producer.ProduceAsync(KafkaTopics.BookingEvents, kafkaMessage, ct);
    }

    public async Task PublishBookingCancelledMessageAsync(BookingCancelled message, CancellationToken ct = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.EventId.ToString(),
            Value = JsonSerializer.Serialize(message),
            Headers = [new Header("message-type", Encoding.UTF8.GetBytes(KafkaMessageTypes.BookingCancelled))]
        };

        await _producer.ProduceAsync(KafkaTopics.BookingEvents, kafkaMessage, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
