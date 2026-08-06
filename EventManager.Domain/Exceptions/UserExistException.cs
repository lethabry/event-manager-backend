namespace EventManager.Domain.Exceptions;

public class UserExistException : Exception
{
    public UserExistException(string message)
        : base(message)
    {
    }
}
