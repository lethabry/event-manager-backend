namespace EventManager.Domain.Exceptions;

public class ActiveBookingLimitException : BookingException
{
    public Guid EventId { get; }

    public ActiveBookingLimitException(Guid eventId)
        : this(eventId, $"Превышено количество активных броней для мероприятия с id = {eventId}")
    {
    }

    public ActiveBookingLimitException(Guid eventId, string message)
        : base(message)
    {
        EventId = eventId;
    }

    public ActiveBookingLimitException(Guid eventId, string message, Exception inner)
        : base(message, inner)
    {
        EventId = eventId;
    }
}
