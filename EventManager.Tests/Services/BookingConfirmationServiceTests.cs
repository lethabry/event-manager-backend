using EventManager.Application.Interfaces;
using EventManager.Application.DTOs;
using EventManager.Application.Services.BookingConfirmationService;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using Moq;

namespace EventManager.Tests.Services;

public class BookingConfirmationServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<IEventService> _mockEventService;
    private readonly BookingConfirmationService _service;

    public BookingConfirmationServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockEventService = new Mock<IEventService>();
        _service = new BookingConfirmationService(_mockEventService.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task ProcessBookingAsync_PendingBooking_UpdatesBookingStatus()
    {
        //Arrange
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var booking = new Booking(evt.Id, Guid.NewGuid());

        _mockEventService.Setup(r => r.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _mockRepository.Setup(r => r.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _mockRepository.Setup(r => r.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        //Act
        await _service.ProcessBookingAsync(booking.Id, CancellationToken.None);

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_MultiplePendingBookings_UpdatesBookingStatuses()
    {
        //Arrange
        var firstEvt = Event.Create("Event 1", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var secondEvt = Event.Create("Event 2", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var thirdEvt = Event.Create("Event 3", DateTime.Now, DateTime.Now.AddHours(2), 5);

        var firstBooking = new Booking(firstEvt.Id, Guid.NewGuid());
        var secondBooking = new Booking(secondEvt.Id, Guid.NewGuid());
        var thirdBooking = new Booking(thirdEvt.Id, Guid.NewGuid());
        List<Booking> pendingBookings = [firstBooking, secondBooking, thirdBooking];

        _mockEventService.Setup(r => r.GetEventByIdAsync(firstEvt.Id)).ReturnsAsync(firstEvt);
        _mockEventService.Setup(r => r.GetEventByIdAsync(secondEvt.Id)).ReturnsAsync(secondEvt);
        _mockEventService.Setup(r => r.GetEventByIdAsync(thirdEvt.Id)).ReturnsAsync(thirdEvt);
        _mockRepository.Setup(r => r.GetBookingsAsync(BookingStatus.Pending))
            .ReturnsAsync(pendingBookings.AsReadOnly());
        _mockRepository.Setup(r => r.GetBookingByIdAsync(firstBooking.Id)).ReturnsAsync(firstBooking);
        _mockRepository.Setup(r => r.GetBookingByIdAsync(secondBooking.Id)).ReturnsAsync(secondBooking);
        _mockRepository.Setup(r => r.GetBookingByIdAsync(thirdBooking.Id)).ReturnsAsync(thirdBooking);
        _mockRepository.Setup(r => r.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking booking, CancellationToken _) => booking);

        //Act
        await _service.ProcessPendingBookingsAsync(CancellationToken.None);

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
        foreach (var booking in pendingBookings)
        {
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
        }
    }

    [Fact]
    public async Task ProcessBookingAsync_ConfirmationSaveFails_ShouldNotRejectOrReleaseSeat()
    {
        //Arrange
        var evt = Event.Create("Test Event", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 1);
        evt.TryReserveSeats();
        var booking = new Booking(evt.Id, Guid.NewGuid());

        _mockEventService.Setup(service => service.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _mockRepository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _mockRepository
            .Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        //Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessBookingAsync(booking.Id, CancellationToken.None));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(0, evt.AvailableSeats);
        _mockRepository.Verify(
            repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockEventService.Verify(
            service => service.UpdateEventAsync(It.IsAny<Guid>(), It.IsAny<EventInfoDTO>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessBookingAsync_ExternalCheckFails_ShouldRejectPendingBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var booking = new Booking(eventId, Guid.NewGuid());

        _mockRepository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _mockEventService
            .Setup(service => service.GetEventByIdAsync(eventId))
            .ThrowsAsync(new EventNotFoundException(eventId));

        //Act
        await _service.ProcessBookingAsync(booking.Id, CancellationToken.None);

        //Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        _mockRepository.Verify(repository => repository.GetBookingByIdAsync(booking.Id), Times.Once);
        _mockRepository.Verify(
            repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessBookingAsync_ExternalCheckFailsForProcessedBooking_ShouldNotPersistAgain()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var booking = new Booking(eventId, Guid.NewGuid());
        booking.Confirm();

        _mockRepository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _mockEventService
            .Setup(service => service.GetEventByIdAsync(eventId))
            .ThrowsAsync(new EventNotFoundException(eventId));

        //Act
        await _service.ProcessBookingAsync(booking.Id, CancellationToken.None);

        //Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        _mockRepository.Verify(
            repository => repository.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
