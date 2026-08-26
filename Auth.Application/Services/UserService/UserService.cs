using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Services.UserValidator;
using EventManager.Common.DTOs;
namespace Auth.Application.Services.UserService;

public class UserService : IUserService
{
    private readonly IUserValidator _validationService;
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository, IUserValidator validationService)
    {
        _userRepository = userRepository;
        _validationService = validationService;
    }

    public async Task RegisterUserAsync(CreatingUserDTO user)
    {
        _validationService.ValidateUser(user);
        await _userRepository.RegisterUserAsync(user);
    }

    public async Task<UserTokenResult> LoginUserAsync(LoginUserDTO user)
    {
        _validationService.ValidateUser(user);
        var token = await _userRepository.LoginUserAsync(user);
        return new UserTokenResult(token);
    }

    public async Task<UserExistingStatus> CheckIfUserExists(Guid userId)
    {
        var status = await _userRepository.CheckIfUserExistAsync(userId);
        return new UserExistingStatus(status);
    }
}
