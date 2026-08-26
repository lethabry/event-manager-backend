namespace EventManager.Contracts.Kafka.Messages.BookingConfirmed;

public record BookingConfirmed
{
    public Guid EventId { get; init; }
    public Guid BookingId { get; init; }
    public Guid UserId { get; init; }
    public int AmountSeats { get; init; }
    public DateTime ProcessedAt { get; init; }
};
