namespace Booking.Domain.Exceptions;

public sealed class EventNotFoundException : EventException
{
    public Guid EventId { get; }

    public EventNotFoundException(Guid eventId)
        : base($"Мероприятие с id {eventId} не найдено")
    {
        EventId = eventId;
    }
}
