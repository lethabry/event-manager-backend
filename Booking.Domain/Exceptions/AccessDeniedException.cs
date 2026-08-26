namespace Booking.Domain.Exceptions;

public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message)
        : base(message)
    {
    }

    public AccessDeniedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
