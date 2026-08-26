using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Services.UserService;
using Auth.Application.Services.UserValidator;
using Auth.Domain.Exceptions;
using EventManager.Common.Enums;
using Moq;

namespace Auth.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserValidator> _validationServiceMock = new();
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userService = new UserService(_userRepositoryMock.Object, _validationServiceMock.Object);
    }

    [Fact]
    public async Task RegisterUser_ValidUser_ValidatesAndPersistsUser()
    {
        //Arrange
        var user = new CreatingUserDTO("new-user", "password", UserRole.User);

        //Act
        await _userService.RegisterUserAsync(user);

        //Assert
        _validationServiceMock.Verify(service => service.ValidateUser(user), Times.Once);
        _userRepositoryMock.Verify(repository => repository.RegisterUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task RegisterUser_InvalidUser_DoesNotPersistUser()
    {
        //Arrange
        var user = new CreatingUserDTO("", "password", UserRole.User);
        _validationServiceMock
            .Setup(service => service.ValidateUser(user))
            .Throws(new UserValidationException("Invalid user"));

        //Act
        var action = () => _userService.RegisterUserAsync(user);

        //Assert
        await Assert.ThrowsAsync<UserValidationException>(action);
        _userRepositoryMock.Verify(repository => repository.RegisterUserAsync(It.IsAny<CreatingUserDTO>()), Times.Never);
    }

    [Fact]
    public async Task LoginUser_ValidCredentials_ReturnsTokenResult()
    {
        //Arrange
        var user = new LoginUserDTO { Login = "existing-user", Password = "password" };
        const string token = "jwt-token";
        _userRepositoryMock.Setup(repository => repository.LoginUserAsync(user)).ReturnsAsync(token);

        //Act
        var result = await _userService.LoginUserAsync(user);

        //Assert
        Assert.Equal(token, result.Token);
        _validationServiceMock.Verify(service => service.ValidateUser(user), Times.Once);
        _userRepositoryMock.Verify(repository => repository.LoginUserAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginUser_InvalidCredentials_PropagatesException()
    {
        //Arrange
        var user = new LoginUserDTO { Login = "existing-user", Password = "wrong-password" };
        _userRepositoryMock
            .Setup(repository => repository.LoginUserAsync(user))
            .ThrowsAsync(new UserValidationException("Неправильные логин или пароль"));

        //Act
        var action = () => _userService.LoginUserAsync(user);

        //Assert
        await Assert.ThrowsAsync<UserValidationException>(action);
    }
}
