using Auth.Application.DTOs;
using Auth.Application.Services.UserService;
using EventManager.Common.DTOs;
using EventManager.Common.Enums;
using Microsoft.AspNetCore.Mvc;
namespace Auth.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Метод для регистрации пользователя
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] CreatingUserBodyDTO user)
    {
        if (!Enum.TryParse<UserRole>(user.Role, true, out var role) || !Enum.IsDefined(role))
        {
            return BadRequest("Роль не валидна");
        }
        var validUser = new CreatingUserDTO(user.Login, user.Password, role);

        await _userService.RegisterUserAsync(validUser);
        return NoContent();
    }

    /// <summary>
    /// Метод для авторизации пользователя
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserTokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginUserDTO user)
    {
        var tokenResult = await _userService.LoginUserAsync(user);
        return Ok(tokenResult);
    }
    
    /// <summary>
    /// Метод для отображения ответа есть ли пользователь с указанным id
    /// </summary>
    /// <param name="id">Id пользователя</param>
    [ProducesResponseType(typeof(UserExistingStatus), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var existing = await _userService.CheckIfUserExists(id);
        return Ok(existing);
    }
}
