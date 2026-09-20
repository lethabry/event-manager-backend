namespace Booking.Infrastructure.Configurations;

public class OpenTelemetryConfiguration
{
    public const string SectionName = "OpenTelemetry";
    
    public string ServiceName { get; set; }
    public string Version { get; set; }
    public string OtlpEndpoint { get; set; }
    public string OtlpProtocol { get; set; }
}
