using EventManager.Application.DTOs;
namespace EventManager.Application.Interfaces;

public interface IUserRepository
{
    public Task RegisterUserAsync(CreatingUserDTO user);
    public Task<string> LoginUserAsync(LogingUserDTO user);
    public Task<bool> CheckIfUserExistAsync(Guid userId);
}
