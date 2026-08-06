using System.Net;
using EventManager.Application.DTOs;
using EventManager.Application.Services.ValidationService;
using EventManager.Domain.Exceptions;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class ValidationServiceTests
{
    private readonly IValidationService _validationService;

    public ValidationServiceTests()
    {
        _validationService = new ValidationService();
    }

    public static IEnumerable<object[]> PaginatedResultValidTestData()
    {
        return
        [
            [new DateTime(2026, 4, 12), new DateTime(2026, 4, 13), 1, 2],
            [new DateTime(2026, 4, 1), DateTime.Now, 2, 2],
            [null, null, 1, 10],
            [null, new DateTime(2026, 5, 1), 5, 20],
            [new DateTime(2026, 5, 1), null, 3, 15],
            [DateTime.Now, DateTime.Now.AddDays(1), 1, 100],
            [DateTime.Now.AddDays(-5), DateTime.Now, 10, 5],
            [null, DateTime.Now.AddDays(30), 999, 1],
            [DateTime.Now, null, 1, 999],
        ];
    }

    public static IEnumerable<object[]> PaginatedResultInvalidDateRangeTestData()
    {
        var now = DateTime.Now;
        return
        [
            [new DateTime(2026, 4, 12), new DateTime(2026, 4, 12), 1, 10],
            [now.AddDays(10), now, 1, 10],
            [now, now, 1, 10],
        ];
    }

    public static IEnumerable<object[]> PaginatedResultInvalidPageTestData()
    {
        return
        [
            [null, null, 0, 10],
            [DateTime.Now, DateTime.Now.AddDays(1), 0, 5],
            [null, null, -1, 10],
            [DateTime.Now, null, -5, 20],
            [null, new DateTime(2026, 5, 1), -100, 15],
            [new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), -1, 10],
            [null, null, int.MinValue, 10],
        ];
    }

    public static IEnumerable<object[]> PaginatedResultInvalidPageSizeTestData()
    {
        return
        [
            [null, null, 1, 0],
            [DateTime.Now, DateTime.Now.AddDays(1), 5, 0],
            [null, null, 1, -1],
            [new DateTime(2026, 4, 1), null, 3, -5],
            [null, new DateTime(2026, 5, 1), 1, -100],
            [new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), 10, -1],
            [null, null, 1, int.MinValue],
        ];
    }

    [Fact]
    public void ValidateEventDTO_ValidEventDTOWithDescription_ShouldReturnNothing()
    {
        // Arrange
        var validEventDTO = new EventInfoDTO()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        //Act
        var result = () => _validationService.ValidateEventDTO(validEventDTO);

        //Assert
        result.Should().NotThrow<EventValidationException>();
    }

    [Fact]
    public void ValidateEventDTO_ValidEventDTOWithoutDescription_ShouldReturnNothing()
    {
        // Arrange
        var validEventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        //Act
        var result = () => _validationService.ValidateEventDTO(validEventDTO);

        //Assert
        result.Should().NotThrow<EventValidationException>();
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsNull_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO
        {
            Title = null,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Название мероприятия не может быть пустым")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsEmptyString_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO
        {
            Title = string.Empty,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Название мероприятия не может быть пустым")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsWhiteSpace_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO
        {
            Title = "   ",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Название мероприятия не может быть пустым")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtIsMinValue_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO
        {
            Title = "Title",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(1),
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Дата начала мероприятия должна быть заполнена")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenEndAtIsMinValue_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue,
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Дата конца мероприятия должна быть заполнена")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtEqualsEndAt_ShouldThrowError()
    {
        // Arrange
        var fixedDate = DateTime.Now;
        var eventDTO = new EventInfoDTO
        {
            Title = "Title",
            StartAt = fixedDate,
            EndAt = fixedDate,
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtIsAfterEndAt_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now.AddHours(2),
            EndAt = DateTime.Now,
            TotalSeats = 1,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenZeroTotalSeats_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 0,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Количество мест должно быть больше 0")
            ;
    }


    [Fact]
    public void ValidateEventDTO_WhenNegativeTotalSeats_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = -5,
            AvailableSeats = 1,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Количество мест должно быть больше 0");
    }

    [Fact]
    public void ValidateEventDTO_WhenNegativeAvailableSeats_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = -5,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Количество доступных мест должно быть не меньше 0")
            ;
    }

    [Fact]
    public void ValidateEventDTO_WhenAvailableSeatsIsBiggerThanTotalSeats_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventInfoDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 10,
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
            .Throw<EventValidationException>()
            .WithMessage("Количество доступных мест не может быть больше мест всего")
            ;
    }

    [Theory]
    [Trait("ValidatePaginatedResult", "Success")]
    [MemberData(nameof(PaginatedResultValidTestData))]
    public void ValidatePaginatedResult_DataValidate_ShouldReturNothing(DateTime? from, DateTime? to, int page, int pageSize)
    {
        // Act
        var result = () => _validationService.ValidatePaginatedResult(from, to, page, pageSize);

        // Assert
        result.Should().NotThrow<EventValidationException>();
    }

    [Theory]
    [Trait("ValidatePaginatedResult", "ThrowException")]
    [MemberData(nameof(PaginatedResultInvalidDateRangeTestData))]
    public void ValidatePaginatedResult_InvalidDateRange_ShouldThrowException(DateTime? from, DateTime? to, int page, int pageSize)
    {
        // Act
        var result = () => _validationService.ValidatePaginatedResult(from, to, page, pageSize);

        // Assert
        result.Should().Throw<EventValidationException>().WithMessage("Дата начала мероприятия должны быть раньше даты окончания мероприятия");
    }

    [Theory]
    [Trait("ValidatePaginatedResult", "ThrowException")]
    [MemberData(nameof(PaginatedResultInvalidPageSizeTestData))]
    public void ValidatePaginatedResult_InvalidPageSize_ShouldThrowException(DateTime? from, DateTime? to, int page, int pageSize)
    {
        // Act
        var result = () => _validationService.ValidatePaginatedResult(from, to, page, pageSize);

        // Assert
        result.Should().Throw<EventValidationException>().WithMessage("Количество элементов не может быть меньше 1");
    }

    [Theory]
    [Trait("ValidatePaginatedResult", "ThrowException")]
    [MemberData(nameof(PaginatedResultInvalidPageTestData))]
    public void ValidatePaginatedResult_InvalidPage_ShouldThrowException(DateTime? from, DateTime? to, int page, int pageSize)
    {
        // Act
        var result = () => _validationService.ValidatePaginatedResult(from, to, page, pageSize);

        // Assert
        result.Should().Throw<EventValidationException>().WithMessage("Номер страницы не может быть меньше 1");
    }

    [Fact]
    [Trait("ValidateUser", "Success")]
    public void ValidateUser_CreatingUserDTO_ValidData_ShouldNotThrow()
    {
        //Arrange
        var user = new CreatingUserDTO("validuser", "password123", Domain.Common.UserRole.User);

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
        var user = new LogingUserDTO { Login = "validuser", Password = "password123" };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().NotThrow<UserValidationException>();
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("")]
    public void ValidateUser_CreatingUserDTO_ShortLogin_ShouldThrowException(string login)
    {
        //Arrange
        var user = new CreatingUserDTO(login, "password123");

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage("Логин слишком короткий");
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("abcde")]
    [InlineData("")]
    public void ValidateUser_CreatingUserDTO_ShortPassword_ShouldThrowException(string password)
    {
        //Arrange
        var user = new CreatingUserDTO("validuser", password);

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage("Пароль слишком короткий");
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("")]
    public void ValidateUser_LogingUserDTO_ShortLogin_ShouldThrowException(string login)
    {
        //Arrange
        var user = new LogingUserDTO { Login = login, Password = "password123" };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage("Логин слишком короткий");
    }

    [Theory]
    [Trait("ValidateUser", "ThrowException")]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("abcde")]
    [InlineData("")]
    public void ValidateUser_LogingUserDTO_ShortPassword_ShouldThrowException(string password)
    {
        //Arrange
        var user = new LogingUserDTO { Login = "validuser", Password = password };

        //Act
        var result = () => _validationService.ValidateUser(user);

        //Assert
        result.Should().Throw<UserValidationException>().WithMessage("Пароль слишком короткий");
    }
}
