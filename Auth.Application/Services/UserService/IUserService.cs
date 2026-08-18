using Auth.Application.DTOs;
namespace Auth.Application.Services.UserService;

public interface IUserService
{
    public Task RegisterUserAsync(CreatingUserDTO user);
    public Task<UserTokenResult> LoginUserAsync(LoginUserDTO user);
}
