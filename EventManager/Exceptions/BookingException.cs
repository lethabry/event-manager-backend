using System.Net;
using EventManager.Models;

namespace EventManager.Exceptions;

public class BookingException : Exception
{
    public HttpStatusCode statusCode { get; }
    public Booking evnt { get; }

    public BookingException()
    {
    }

    public BookingException(HttpStatusCode code, string message, Booking? e = null)
        : base(message)
    {
        evnt = e;
        statusCode = code;
    }

    public BookingException(HttpStatusCode code, string message, Booking e, Exception inner)
        : base(message, inner)
    {
        evnt = e;
        statusCode = code;
    }
}