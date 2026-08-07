using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.ValidationService;
namespace EventManager.Application.Services.UserService;

public class UserService : IUserService
{
    private readonly IValidationService _validationService;
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository, IValidationService validationService)
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
}
