using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace EventManager.Infrastructure.Repositories.UserRepository;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _appDbContext;
    private readonly ILogger<UserRepository> _logger;
    private readonly IHasher _hasher;
    private readonly ITokenGenerator _tokenGenerator;

    public UserRepository(AppDbContext appDbContext, IHasher hasher, ITokenGenerator tokenGenerator)
        : this(appDbContext, NullLogger<UserRepository>.Instance, hasher, tokenGenerator)
    {
    }

    public UserRepository(AppDbContext appDbContext, ILogger<UserRepository> logger, IHasher hasher, ITokenGenerator tokenGenerator)
    {
        _appDbContext = appDbContext;
        _logger = logger;
        _hasher = hasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task RegisterUserAsync(CreatingUserDTO user)
    {
        _logger.LogInformation("Start creating user in database");

        var isExist = await _appDbContext.Users.AnyAsync(u => u.Login == user.Login);
        if (isExist)
        {
            throw new UserExistException("Пользователь с таким логином уже существует");
        }

        var passwordHash = _hasher.GetHash(user.Password);
        var createdUser = new User(user.Login, passwordHash, user.Role);
        await _appDbContext.Users.AddAsync(createdUser);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<string> LoginUserAsync(LogingUserDTO user)
    {
        _logger.LogInformation("Start finding user in database");
        var existUser = await _appDbContext.Users.SingleOrDefaultAsync(u => u.Login == user.Login);
        if (existUser == null)
        {
            throw new UserValidationException("Неправильные логин или пароль");
        }

        var passwordHash = _hasher.GetHash(user.Password);
        if (existUser.PasswordHash != passwordHash)
        {
            throw new UserValidationException("Неправильные логин или пароль");
        }

        var userTokenInfo = new UserDTO()
        {
            Id = existUser.Id,
            Login = existUser.Login,
            Role = existUser.Role,
        };
        var token = _tokenGenerator.GenerateToken(userTokenInfo);
        return token;
    }
}
