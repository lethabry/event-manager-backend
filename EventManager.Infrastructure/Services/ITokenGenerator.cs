using EventManager.Application.DTOs;
namespace EventManager.Infrastructure.Services;

public interface ITokenGenerator
{
    public string GenerateToken(UserDTO user);
}
