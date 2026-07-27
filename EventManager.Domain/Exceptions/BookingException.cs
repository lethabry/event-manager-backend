using System.Net;
using EventManager.Domain.Models;

namespace EventManager.Domain.Exceptions;

public class BookingException : Exception
{
    public HttpStatusCode statusCode { get; }
    public Booking? booking { get; }

    public BookingException()
    {
    }

    public BookingException(HttpStatusCode code, string message, Booking? b = null)
        : base(message)
    {
        booking = b;
        statusCode = code;
    }

    public BookingException(HttpStatusCode code, string message, Booking b, Exception inner)
        : base(message, inner)
    {
        booking = b;
        statusCode = code;
    }
}