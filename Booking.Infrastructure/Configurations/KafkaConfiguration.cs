namespace Booking.Infrastructure.Configurations;

public class KafkaConfiguration
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = string.Empty;
}
