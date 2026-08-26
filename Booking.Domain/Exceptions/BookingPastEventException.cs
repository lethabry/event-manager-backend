namespace Booking.Domain.Exceptions;

public class BookingPastEventException : BookingException
{
    public Guid EventId { get; }

    public BookingPastEventException(Guid eventId)
        : this(eventId, "Нельзя забронировать прошедшее мероприятие")
    {
    }

    public BookingPastEventException(Guid eventId, string message)
        : base(message)
    {
        EventId = eventId;
    }

    public BookingPastEventException(Guid eventId, string message, Exception inner)
        : base(message, inner)
    {
        EventId = eventId;
    }

}
