namespace EventManager.Contracts.Kafka.Messages.BookingConfirmed;

public record BookingConfirmed(
    Guid EventId,
    Guid BookingId,
    Guid UserId,
    int AmountSeats,
    DateTime UpdatedAt
);
