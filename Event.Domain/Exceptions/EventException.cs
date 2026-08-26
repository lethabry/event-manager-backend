namespace Event.Domain.Exceptions;

public class EventException : Exception
{
    public EventException(string message)
        : base(message)
    {
    }

    public EventException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
