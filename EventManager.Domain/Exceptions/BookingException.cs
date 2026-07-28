namespace EventManager.Domain.Exceptions;

public class BookingException : Exception
{
    public int statusCode { get; }
    public Guid? BookingId { get; }

    public BookingException()
    {
    }

    public BookingException(int code, string message, Guid? bookingId = null)
        : base(message)
    {
        statusCode = code;
        BookingId = bookingId;
    }

    public BookingException(int code, string message, Guid bookingId, Exception inner)
        : base(message, inner)
    {
        statusCode = code;
        BookingId = bookingId;
    }
}