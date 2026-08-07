using EventManager.Application.DTOs;
namespace EventManager.Application.Interfaces;

public interface ITokenGenerator
{
    public string GenerateToken(UserDTO user);
}
