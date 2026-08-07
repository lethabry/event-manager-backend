namespace EventManager.Domain.Exceptions;

public class UserValidationException : Exception
{
    public UserValidationException(string message)
        : base(message)
    {
    }

}
