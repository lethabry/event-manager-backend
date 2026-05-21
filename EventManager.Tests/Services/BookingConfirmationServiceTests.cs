using EventManager.BackgroundServices;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.Tests.Services;

public class BookingConfirmationServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<ILogger<BookingConfirmationService>> _mockLogger;
    private readonly BookingConfirmationService _service;

    public BookingConfirmationServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockLogger = new Mock<ILogger<BookingConfirmationService>>();
        _service = new BookingConfirmationService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePendingBooking_UpdateBookingStatus()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var booking = new Booking(eventId);
        List<Booking> pendingBookings = [booking];

        var ct = new CancellationTokenSource();
        _mockRepository.Setup((r) => r.GetBookings(BookingStatus.Pending))
                       .ReturnsAsync(pendingBookings.AsReadOnly());

        //Act
        var exucuteTask = _service.StartAsync(ct.Token);
        await Task.Delay(2000);
        ct.Cancel();
        await exucuteTask;

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBooking(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MultiplePendingBooking_UpdateBookingStatus()
    {
        //Arrange
        var firstEventId = Guid.NewGuid();
        var secondEventId = Guid.NewGuid();
        var thirdEventId = Guid.NewGuid();

        var firstBooking = new Booking(firstEventId);
        var secondBooking = new Booking(secondEventId);
        var thirdBooking = new Booking(thirdEventId);
        List<Booking> pendingBookings = [firstBooking, secondBooking, thirdBooking];
        _mockRepository.Setup((r) => r.GetBookings(BookingStatus.Pending))
                       .ReturnsAsync(pendingBookings.AsReadOnly());
        var ct = new CancellationTokenSource();

        //Act
        var exucuteTask = _service.StartAsync(ct.Token);
        await Task.Delay(10000);
        ct.Cancel();
        await exucuteTask;

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBooking(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2)
        );
        foreach (var booking in pendingBookings)
        {
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
        }
    }
}