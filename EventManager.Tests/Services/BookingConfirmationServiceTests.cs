using EventManager.BackgroundServices;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Data.EventRepository;
using EventManager.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.Tests.Services;

public class BookingConfirmationServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<ILogger<BookingConfirmationService>> _mockLogger;
    private readonly BookingConfirmationService _service;
    private readonly Mock<IEventRepository> _mockEventRepository;

    public BookingConfirmationServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockLogger = new Mock<ILogger<BookingConfirmationService>>();
        _mockEventRepository = new Mock<IEventRepository>();
        _service = new BookingConfirmationService(_mockRepository.Object, _mockLogger.Object, _mockEventRepository.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePendingBooking_UpdateBookingStatus()
    {
        //Arrange
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var booking = new Booking(evt.Id);
        List<Booking> pendingBookings = [booking];

        var ct = new CancellationTokenSource();
        _mockEventRepository.Setup(r => r.GetEventById(evt.Id)).Returns(evt);
        _mockRepository.Setup(r => r.GetBookings(BookingStatus.Pending))
            .ReturnsAsync(pendingBookings.AsReadOnly());
        _mockRepository.Setup(r => r.UpdateBooking(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

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
        var firstEvt = Event.Create("Event 1", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var secondEvt = Event.Create("Event 2", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var thirdEvt = Event.Create("Event 3", DateTime.Now, DateTime.Now.AddHours(2), 5);

        var firstBooking = new Booking(firstEvt.Id);
        var secondBooking = new Booking(secondEvt.Id);
        var thirdBooking = new Booking(thirdEvt.Id);
        List<Booking> pendingBookings = [firstBooking, secondBooking, thirdBooking];
        
        var ct = new CancellationTokenSource();
        _mockEventRepository.Setup(r => r.GetEventById(firstEvt.Id)).Returns(firstEvt);
        _mockEventRepository.Setup(r => r.GetEventById(secondEvt.Id)).Returns(secondEvt);
        _mockEventRepository.Setup(r => r.GetEventById(thirdEvt.Id)).Returns(thirdEvt);
        _mockRepository.Setup(r => r.GetBookings(BookingStatus.Pending))
            .ReturnsAsync(pendingBookings.AsReadOnly());
        _mockRepository.Setup(r => r.UpdateBooking(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking b, CancellationToken _) => b);

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
