using Auth.Application.DTOs;
using Auth.Application.Services.UserValidator;
using Auth.Domain.Exceptions;
using FluentAssertions;

namespace Auth.UnitTests.Services;

public class UserValidatorServiceTests
{
    private readonly IUserValidator _validationService;

    public UserValidatorServiceTests()
    {
        _validationService = new UserValidator();
    }

    [Fact]
    [Trait("ValidateUser", "Success")]
    public void ValidateUser_CreatingUserDTO_ValidData_ShouldNotThrow()
    {
        //Arrange
        var user = new CreatingUserDTO("validuser", "password123");

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().NotThrow<UserValidationException>();
    }

    [Fact]
    [Trait("ValidateUser", "Success")]
    public void ValidateUser_LoginUserDTO_ValidData_ShouldNotThrow()
    {
        //Arrange
        var user = new LoginUserDTO { Login = "validuser", Password = "password123" };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().NotThrow<UserValidationException>();
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a", "Логин слишком короткий")]
    [InlineData("ab", "Логин слишком короткий")]
    [InlineData("", "Логин не может быть пустым")]
    public void ValidateUser_CreatingUserDTO_ShortLogin_ShouldThrowException(string login, string message)
    {
        //Arrange
        var user = new CreatingUserDTO(login, "password123");

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage(message);
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a", "Пароль слишком короткий")]
    [InlineData("abc", "Пароль слишком короткий")]
    [InlineData("abcde", "Пароль слишком короткий")]
    [InlineData("", "Пароль не может быть пустым")]
    public void ValidateUser_CreatingUserDTO_ShortPassword_ShouldThrowException(string password, string message)
    {
        //Arrange
        var user = new CreatingUserDTO("validuser", password);

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage(message);
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a", "Логин слишком короткий")]
    [InlineData("ab", "Логин слишком короткий")]
    [InlineData("", "Логин не может быть пустым")]
    public void ValidateUser_LoginUserDTO_ShortLogin_ShouldThrowException(string login, string message)
    {
        //Arrange
        var user = new LoginUserDTO { Login = login, Password = "password123" };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage(message);
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a", "Пароль слишком короткий")]
    [InlineData("abc", "Пароль слишком короткий")]
    [InlineData("abcde", "Пароль слишком короткий")]
    [InlineData("", "Пароль не может быть пустым")]
    public void ValidateUser_LoginUserDTO_ShortPassword_ShouldThrowException(string password, string message)
    {
        //Arrange
        var user = new LoginUserDTO { Login = "validuser", Password = password };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage(message);
    }
}
