using EventManager.Application.DTOs;
namespace EventManager.Application.Services.UserService;

public interface IUserService
{
    public Task<UserTokenResult> RegisterUserAsync(CreatingUserDTO user);
    public Task<UserTokenResult> LoginUserAsync(LogingUserDTO user);
}
