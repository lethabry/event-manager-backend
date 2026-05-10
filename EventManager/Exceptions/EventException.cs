using System.Net;
using EventManager.Models;

namespace EventManager.Exceptions;

public class EventException : Exception
{
    public HttpStatusCode statusCode { get; }
    public Event evnt { get; }

    public EventException()
    {
    }

    public EventException(HttpStatusCode code, string message, Event? e = null)
        : base(message)
    {
        evnt = e;
        statusCode = code;
    }

    public EventException(HttpStatusCode code, string message, Event e, Exception inner)
        : base(message, inner)
    {
        evnt = e;
        statusCode = code;
    }
}