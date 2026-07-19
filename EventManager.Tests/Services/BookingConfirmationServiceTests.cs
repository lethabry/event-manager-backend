using EventManager.BackgroundServices;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Models;
using EventManager.Services.EventService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventManager.Tests.Services;

public class BookingConfirmationServiceTests
{
    private readonly Mock<IBookingRepository> _mockRepository;
    private readonly Mock<ILogger<BookingConfirmationService>> _mockLogger;
    private readonly BookingConfirmationService _service;
    private readonly Mock<IEventService> _mockEventService;

    public BookingConfirmationServiceTests()
    {
        _mockRepository = new Mock<IBookingRepository>();
        _mockLogger = new Mock<ILogger<BookingConfirmationService>>();
        _mockEventService = new Mock<IEventService>();

        var services = new ServiceCollection();
        services.AddScoped(_ => _mockEventService.Object);
        services.AddScoped(_ => _mockRepository.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _service = new BookingConfirmationService(_mockLogger.Object, scopeFactory);
    }

    [Fact]
    public async Task ExecuteAsync_SinglePendingBooking_UpdateBookingStatus()
    {
        //Arrange
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var booking = new Booking(evt.Id);
        List<Booking> pendingBookings = [booking];

        var ct = new CancellationTokenSource();
        _mockEventService.Setup(r => r.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _mockRepository.Setup(r => r.GetBookingsAsync(BookingStatus.Pending))
            .ReturnsAsync(pendingBookings.AsReadOnly());
        _mockRepository.Setup(r => r.GetBookingByIdAsync(booking.Id))
            .ReturnsAsync(booking);
        _mockRepository.Setup(r => r.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        //Act
        var exucuteTask = _service.StartAsync(ct.Token);
        await Task.Delay(AppConstants.DelayBetweenBookingConfirmationHandling + 1000);
        ct.Cancel();
        await exucuteTask;

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce
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
        _mockEventService.Setup(r => r.GetEventByIdAsync(firstEvt.Id)).ReturnsAsync(firstEvt);
        _mockEventService.Setup(r => r.GetEventByIdAsync(secondEvt.Id)).ReturnsAsync(secondEvt);
        _mockEventService.Setup(r => r.GetEventByIdAsync(thirdEvt.Id)).ReturnsAsync(thirdEvt);
        _mockRepository.Setup(r => r.GetBookingsAsync(BookingStatus.Pending))
            .ReturnsAsync(pendingBookings.AsReadOnly());
        _mockRepository.Setup(r => r.GetBookingByIdAsync(firstBooking.Id)).ReturnsAsync(firstBooking);
        _mockRepository.Setup(r => r.GetBookingByIdAsync(secondBooking.Id)).ReturnsAsync(secondBooking);
        _mockRepository.Setup(r => r.GetBookingByIdAsync(thirdBooking.Id)).ReturnsAsync(thirdBooking);
        _mockRepository.Setup(r => r.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking b, CancellationToken _) => b);

        //Act
        var exucuteTask = _service.StartAsync(ct.Token);
        await Task.Delay(AppConstants.DelayBetweenBookingConfirmationHandling + 2000);
        ct.Cancel();
        await exucuteTask;

        //Assert
        _mockRepository.Verify(
            repo => repo.UpdateBookingAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2)
        );
        foreach (var booking in pendingBookings)
        {
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
        }
    }
}
