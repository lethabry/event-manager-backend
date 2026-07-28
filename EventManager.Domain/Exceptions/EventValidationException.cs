namespace EventManager.Domain.Exceptions;

public class EventValidationException : EventException
{
    public EventValidationException(string message)
        : base(400, message)
    {
    }
}