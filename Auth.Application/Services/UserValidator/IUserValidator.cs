using Auth.Application.DTOs;
namespace Auth.Application.Services.UserValidator;

public interface IUserValidator
{
    void ValidateUser(CreatingUserDTO userDTO);
    void ValidateUser(LoginUserDTO userDTO);
}
