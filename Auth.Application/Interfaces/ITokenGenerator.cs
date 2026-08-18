using Auth.Application.DTOs;
namespace Auth.Application.Interfaces;

public interface ITokenGenerator
{
    public string GenerateToken(UserDTO user);
}
