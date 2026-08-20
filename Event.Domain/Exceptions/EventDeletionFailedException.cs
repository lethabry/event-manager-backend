namespace Event.Domain.Exceptions;

public sealed class EventDeletionFailedException : EventException
{
    public EventDeletionFailedException()
        : base("Не удалось удалить мероприятие")
    {
    }
}
