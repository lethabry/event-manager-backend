namespace EventManager.Domain.Exceptions;

public class EventException : Exception
{
    public int statusCode { get; }
    public Guid? EventId { get; }

    public EventException()
    {
    }

    public EventException(int code, string message, Guid? eventId = null)
        : base(message)
    {
        statusCode = code;
        EventId = eventId;
    }

    public EventException(int code, string message, Guid eventId, Exception inner)
        : base(message, inner)
    {
        statusCode = code;
        EventId = eventId;
    }
}