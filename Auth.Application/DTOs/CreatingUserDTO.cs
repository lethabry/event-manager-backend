using EventManager.Contracts.Common;
namespace Auth.Application.DTOs;

public sealed record CreatingUserDTO(
    string Login,
    string Password,
    UserRole Role = UserRole.User);
