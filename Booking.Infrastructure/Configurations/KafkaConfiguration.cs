namespace Booking.Infrastructure.Configurations;

public class KafkaConfiguration
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = string.Empty;
    public string ConsumerGroup { get; init; } = string.Empty;
    public bool EnableAutoCommit { get; set; }
}
