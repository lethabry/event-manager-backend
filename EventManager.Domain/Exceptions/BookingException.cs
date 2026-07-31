namespace EventManager.Domain.Exceptions;

public class BookingException : Exception
{
    public BookingException(string message)
        : base(message)
    {
    }

    public BookingException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
