using EventManager.Application.Interfaces;
using EventManager.Application.Services.BookingService;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using FluentAssertions;
using Moq;

namespace EventManager.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _userRepositoryMock.Setup(repository => repository.CheckIfUserExistAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _bookingService = new BookingService(_bookingRepositoryMock.Object, _eventRepositoryMock.Object, _userRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateBooking_RepositoryReturnsBooking_ShouldReturnDto()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = new Booking(eventId, userId);
        var evt = Event.Create("Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync(evt);
        _bookingRepositoryMock.Setup(repository => repository.GetCountOfActiveBookingsAsync(userId)).ReturnsAsync(0);
        _bookingRepositoryMock.Setup(repository => repository.CreateBookingAsync(eventId, userId)).ReturnsAsync(booking);

        var result = await _bookingService.CreateBookingAsync(eventId, userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be("Pending");
        result.ProcessedAt.Should().BeNull();
        _bookingRepositoryMock.Verify(repository => repository.CreateBookingAsync(eventId, userId), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_NoAvailableSeats_ShouldPropagateException()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = Event.Create("Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        var expected = new NoAvailableSeatsException(eventId, "No seats");
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync(evt);
        _bookingRepositoryMock.Setup(repository => repository.GetCountOfActiveBookingsAsync(userId)).ReturnsAsync(0);
        _bookingRepositoryMock.Setup(repository => repository.CreateBookingAsync(eventId, userId)).ThrowsAsync(expected);

        var action = () => _bookingService.CreateBookingAsync(eventId, userId);

        await action.Should().ThrowAsync<NoAvailableSeatsException>().Where(b => b.EventId == eventId);
    }

    [Fact]
    public async Task CreateBooking_EventNotFound_ShouldThrowException()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync((Event?)null);

        //Act
        var action = () => _bookingService.CreateBookingAsync(eventId, userId);

        //Assert
        await action.Should().ThrowAsync<EventNotFoundException>().Where(b => b.EventId == eventId);
    }

    [Fact]
    public async Task CreateBooking_PastEvent_ShouldThrowException()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = Event.Create("Event", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), 1);
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync(evt);

        //Act
        var action = () => _bookingService.CreateBookingAsync(eventId, userId);

        //Assert
        await action.Should().ThrowAsync<BookingPastEventException>().Where(b => b.EventId == eventId);
        _bookingRepositoryMock.Verify(repository => repository.CreateBookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_UserNotFound_ShouldThrowException()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = Event.Create("Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync(evt);
        _userRepositoryMock.Setup(repository => repository.CheckIfUserExistAsync(userId)).ReturnsAsync(false);

        //Act
        var action = () => _bookingService.CreateBookingAsync(eventId, userId);

        //Assert
        await action.Should().ThrowAsync<UserNotFoundException>();
        _bookingRepositoryMock.Verify(repository => repository.CreateBookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateBooking_ActiveBookingLimitReached_ShouldThrowException()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = Event.Create("Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 1);
        _eventRepositoryMock.Setup(repository => repository.GetEventByIdAsync(eventId)).ReturnsAsync(evt);
        _bookingRepositoryMock.Setup(repository => repository.GetCountOfActiveBookingsAsync(userId))
            .ReturnsAsync(AppConstants.MaxActiveBookings);

        //Act
        var action = () => _bookingService.CreateBookingAsync(eventId, userId);

        //Assert
        await action.Should().ThrowAsync<ActiveBookingLimitException>().Where(b => b.UserId == userId);
        _bookingRepositoryMock.Verify(repository => repository.CreateBookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelBooking_OtherUsersBookingByUser_ShouldThrowAccessDeniedException()
    {
        //Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        var requestingUserId = Guid.NewGuid();
        _bookingRepositoryMock.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var action = () => _bookingService.CancelBookingAsync(booking.Id, requestingUserId, UserRole.User);

        //Assert
        await action.Should().ThrowAsync<AccessDeniedException>()
            .WithMessage("Отменять можно только собственные бронирования");
        _bookingRepositoryMock.Verify(repository => repository.CancelBookingAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingById_BookingExists_ShouldReturnDto()
    {
        //Arrange
        var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());
        _bookingRepositoryMock.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(booking.EventId);
        result.Status.Should().Be("Pending");
        _bookingRepositoryMock.Verify(repository => repository.GetBookingByIdAsync(booking.Id), Times.Once);
    }

    [Fact]
    public async Task GetBookingById_BookingNotFound_ShouldThrowBookingException()
    {
        //Arrange
        var bookingId = Guid.NewGuid();
        _bookingRepositoryMock
            .Setup(repository => repository.GetBookingByIdAsync(bookingId))
            .ReturnsAsync((Booking?)null);

        //Act
        var action = () => _bookingService.GetBookingByIdAsync(bookingId);

        //Assert
        await action.Should().ThrowAsync<BookingNotFoundException>().Where(b => b.BookingId == bookingId);
    }
}
