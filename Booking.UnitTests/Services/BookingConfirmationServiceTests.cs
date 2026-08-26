using Booking.Application.Interfaces;
using Booking.Application.Services.BookingConfirmationService;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using Moq;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.UnitTests.Services;

public class BookingConfirmationServiceTests
{
    private readonly Mock<IBookingRepository> _repository = new();
    private readonly Mock<IBookingProducer> _producer = new();
    private readonly BookingConfirmationService _service;

    public BookingConfirmationServiceTests()
    {
        _service = new BookingConfirmationService(_repository.Object, _producer.Object);
    }

    [Fact]
    public async Task ProcessBookingAsync_PendingBooking_ConfirmsSavesAndPublishes()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(booking.UserId)).ReturnsAsync(1);
        _repository.Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        _producer.Setup(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.ProcessBookingAsync(booking.Id, CancellationToken.None);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        _repository.Verify(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(producer => producer.PublishBookingConfirmedMessageAsync(
            It.Is<BookingConfirmed>(message =>
                message.BookingId == booking.Id &&
                message.EventId == booking.EventId &&
                message.UserId == booking.UserId &&
                message.AmountSeats == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingBookingsAsync_MultipleBookings_ProcessesEveryBooking()
    {
        var bookings = new[]
        {
            new BookingEntity(Guid.NewGuid(), Guid.NewGuid()),
            new BookingEntity(Guid.NewGuid(), Guid.NewGuid()),
            new BookingEntity(Guid.NewGuid(), Guid.NewGuid())
        };

        _repository.Setup(repository => repository.GetBookingsAsync(BookingStatus.Pending)).ReturnsAsync(bookings);
        foreach (var booking in bookings)
        {
            _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
            _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(booking.UserId)).ReturnsAsync(1);
        }
        _repository.Setup(repository => repository.UpdateBookingAsync(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingEntity booking, CancellationToken _) => booking);
        _producer.Setup(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.ProcessPendingBookingsAsync(CancellationToken.None);

        Assert.All(bookings, booking => Assert.Equal(BookingStatus.Confirmed, booking.Status));
        _producer.Verify(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessBookingAsync_BookingNotFound_ThrowsException()
    {
        var bookingId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetBookingByIdAsync(bookingId)).ReturnsAsync((BookingEntity?)null);

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _service.ProcessBookingAsync(bookingId, CancellationToken.None));

        _producer.Verify(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBookingAsync_ActiveBookingLimitExceeded_RejectsWithoutPublishing()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(booking.UserId)).ReturnsAsync(AppConstants.MaxActiveBookings + 1);
        _repository.Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _service.ProcessBookingAsync(booking.Id, CancellationToken.None);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        _producer.Verify(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBookingAsync_SaveFails_DoesNotPublish()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(booking.UserId)).ReturnsAsync(1);
        _repository.Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ProcessBookingAsync(booking.Id, CancellationToken.None));

        _producer.Verify(producer => producer.PublishBookingConfirmedMessageAsync(It.IsAny<BookingConfirmed>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
