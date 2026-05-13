using System.Net;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.ValidationService;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class ValidationServiceTests
{
    private readonly IValidationService _validationService;

    public ValidationServiceTests()
    {
        _validationService = new ValidationService();
    }

    [Fact]
    public void ValidateEventDTO_ValidEventDTOWithDescription_ShouldReturnNothing()
    {
        // Arrange
        var validEventDTO = new EventDTO()
        {
            Title = "Title",
            Description = "Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
        };

        //Act
        var result = () => _validationService.ValidateEventDTO(validEventDTO);

        //Assert
        result.Should().NotThrow<EventException>();
    }

    [Fact]
    public void ValidateEventDTO_ValidEventDTOWithoutDescription_ShouldReturnNothing()
    {
        // Arrange
        var validEventDTO = new EventDTO()
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1),
        };

        //Act
        var result = () => _validationService.ValidateEventDTO(validEventDTO);

        //Assert
        result.Should().NotThrow<EventException>();
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsNull_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = null,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Название мероприятия не может быть пустым")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsEmptyString_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = string.Empty,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Название мероприятия не может быть пустым")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenTitleIsWhiteSpace_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = "   ",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Название мероприятия не может быть пустым")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtIsMinValue_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = "Title",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(1)
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата начала мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenEndAtIsMinValue_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = "Title",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата конца мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtEqualsEndAt_ShouldThrowError()
    {
        // Arrange
        var fixedDate = DateTime.Now;
        var eventDTO = new EventDTO
        {
            Title = "Title",
            StartAt = fixedDate,
            EndAt = fixedDate
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ValidateEventDTO_WhenStartAtIsAfterEndAt_ShouldThrowError()
    {
        // Arrange
        var eventDTO = new EventDTO
        {
            Title = "Title",
            StartAt = DateTime.Now.AddHours(2),
            EndAt = DateTime.Now
        };

        // Act
        var result = () => _validationService.ValidateEventDTO(eventDTO);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
    }
}