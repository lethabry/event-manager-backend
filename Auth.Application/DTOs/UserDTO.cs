using EventManager.Contracts.Common;
namespace Auth.Application.DTOs;

public record UserDTO()
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; }
};
