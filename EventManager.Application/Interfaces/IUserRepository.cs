using EventManager.Application.DTOs;
namespace EventManager.Application.Interfaces;

public interface IUserRepository
{
    public Task<string> RegisterUserAsync(CreatingUserDTO user);
    public Task<string> LoginUserAsync(LogingUserDTO user);
}
