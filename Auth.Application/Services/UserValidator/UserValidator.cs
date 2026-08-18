using Auth.Application.DTOs;
using Auth.Domain.Exceptions;
namespace Auth.Application.Services.UserValidator;

public class UserValidator : IUserValidator
{
    public void ValidateUser(CreatingUserDTO user)
    {
        if (string.IsNullOrEmpty(user.Login))
        {
            throw new UserValidationException("Логин не может быть пустым");
        }

        if (user.Login.Length < 3)
        {
            throw new UserValidationException("Логин слишком короткий");
        }

        if (string.IsNullOrEmpty(user.Password))
        {
            throw new UserValidationException("Пароль не может быть пустым");
        }

        if (user.Password.Length < 6)
        {
            throw new UserValidationException("Пароль слишком короткий");
        }
    }

    public void ValidateUser(LoginUserDTO user)
    {
        if (string.IsNullOrEmpty(user.Login))
        {
            throw new UserValidationException("Логин не может быть пустым");
        }

        if (user.Login.Length < 3)
        {
            throw new UserValidationException("Логин слишком короткий");
        }

        if (string.IsNullOrEmpty(user.Password))
        {
            throw new UserValidationException("Пароль не может быть пустым");
        }

        if (user.Password.Length < 6)
        {
            throw new UserValidationException("Пароль слишком короткий");
        }
    }
}
