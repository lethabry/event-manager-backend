using EventManager.Domain.Common;
namespace EventManager.Application.DTOs;

public record CreatingUserDTO(string login, string password, UserRole? role = null)
{
    public string Login { get; private set; } = login;
    public string Password { get; private set; } = password;
    public UserRole? Role { get; private set; } = UserRole.User;
};
