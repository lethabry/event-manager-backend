namespace Booking.Domain.Exceptions;

public class NoAvailableSeatsException : BookingException
{
    public Guid EventId { get; }

    public NoAvailableSeatsException(Guid eventId)
        : this(eventId, "Нет свободных мест")
    {
    }

    public NoAvailableSeatsException(Guid eventId, string message)
        : base(message)
    {
        EventId = eventId;
    }

    public NoAvailableSeatsException(Guid eventId, string message, Exception inner)
        : base(message, inner)
    {
        EventId = eventId;
    }
}
