using EventManager.Domain.Common;
namespace EventManager.Application.DTOs;

public record UserForTokenDTO()
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public UserRole Role { get; set; }
};
