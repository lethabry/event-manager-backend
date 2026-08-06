using EventManager.Domain.Common;
namespace EventManager.Application.DTOs;

public sealed record CreatingUserDTO(
    string Login,
    string Password,
    UserRole Role = UserRole.User);
