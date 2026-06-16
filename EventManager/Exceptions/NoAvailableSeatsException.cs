using System.Net;
using EventManager.Models;
namespace EventManager.Exceptions;

public class NoAvailableSeatsException : Exception
{
    public HttpStatusCode statusCode { get; }

    public NoAvailableSeatsException()
    {
    }

    public NoAvailableSeatsException(HttpStatusCode code, string message)
        : base(message)
    {
        statusCode = code;
    }

    public NoAvailableSeatsException(HttpStatusCode code, string message, Booking b, Exception inner)
        : base(message, inner)
    {
        statusCode = code;
    }
}
