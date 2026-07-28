namespace EventManager.Domain.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public int statusCode { get; }
    public Guid? EventId { get; }

    public NoAvailableSeatsException()
    {
    }

    public NoAvailableSeatsException(int code, string message, Guid? eventId = null)
        : base(message)
    {
        statusCode = code;
        EventId = eventId;
    }

    public NoAvailableSeatsException(int code, string message, Guid eventId, Exception inner)
        : base(message, inner)
    {
        statusCode = code;
        EventId = eventId;
    }
}