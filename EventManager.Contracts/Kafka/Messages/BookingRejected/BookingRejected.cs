namespace EventManager.Contracts.Kafka.Messages.BookingRejected;

public record BookingRejected
{
    public Guid EventId { get; init; }
    public Guid BookingId { get; init; }
    public Guid UserId { get; init; }
    public int AmountSeats { get; init; }
};
