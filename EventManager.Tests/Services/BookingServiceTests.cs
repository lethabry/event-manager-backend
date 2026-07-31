using EventManager.Application.Interfaces;
using EventManager.Application.Services.BookingService;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using FluentAssertions;
using Moq;

namespace EventManager.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _bookingService = new BookingService(_bookingRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateBooking_RepositoryReturnsBooking_ShouldReturnDto()
    {
        var eventId = Guid.NewGuid();
        var booking = new Booking(eventId);
        _bookingRepositoryMock.Setup(repository => repository.CreateBookingAsync(eventId)).ReturnsAsync(booking);

        var result = await _bookingService.CreateBookingAsync(eventId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be("Pending");
        result.ProcessedAt.Should().BeNull();
        _bookingRepositoryMock.Verify(repository => repository.CreateBookingAsync(eventId), Times.Once);
    }

    [Fact]
    public async Task CreateBooking_NoAvailableSeats_ShouldPropagateException()
    {
        var eventId = Guid.NewGuid();
        var expected = new NoAvailableSeatsException(eventId, "No seats");
        _bookingRepositoryMock.Setup(repository => repository.CreateBookingAsync(eventId)).ThrowsAsync(expected);

        var action = () => _bookingService.CreateBookingAsync(eventId);

        await action.Should().ThrowAsync<NoAvailableSeatsException>().Where(b => b.EventId == eventId);
    }

    [Fact]
    public async Task CreateBooking_EventNotFound_ShouldPropagateException()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var expected = new EventNotFoundException(eventId);
        _bookingRepositoryMock.Setup(repository => repository.CreateBookingAsync(eventId)).ThrowsAsync(expected);

        //Act
        var action = () => _bookingService.CreateBookingAsync(eventId);

        //Assert
        await action.Should().ThrowAsync<EventNotFoundException>().Where(b => b.EventId == eventId);
    }

    [Fact]
    public async Task GetBookingById_BookingExists_ShouldReturnDto()
    {
        //Arrange
        var booking = new Booking(Guid.NewGuid());
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
