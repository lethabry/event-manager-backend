namespace EventManager.Domain.Exceptions;

public class ActiveBookingLimitException : BookingException
{
    public Guid UserId { get; }

    public ActiveBookingLimitException(Guid userId)
        : this(userId, $"Превышено количество активных броней для пользователя с id = {userId}")
    {
    }

    public ActiveBookingLimitException(Guid userId, string message)
        : base(message)
    {
        UserId = userId;
    }

    public ActiveBookingLimitException(Guid userId, string message, Exception inner)
        : base(message, inner)
    {
        UserId = userId;
    }
}
