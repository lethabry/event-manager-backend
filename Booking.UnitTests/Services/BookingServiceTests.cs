using Booking.Application.Interfaces;
using Booking.Application.Services.BookingService;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Common.Enums;
using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using Moq;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.UnitTests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _repository = new();
    private readonly Mock<IBookingProducer> _producer = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(_repository.Object, _producer.Object);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsDto()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        var result = await _service.GetBookingByIdAsync(booking.Id);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending.ToString(), result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_MissingBooking_ThrowsException()
    {
        var bookingId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetBookingByIdAsync(bookingId)).ReturnsAsync((BookingEntity?)null);

        await Assert.ThrowsAsync<BookingNotFoundException>(() => _service.GetBookingByIdAsync(bookingId));
    }

    [Fact]
    public async Task CreateBookingAsync_WithinLimit_CreatesBooking()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = new BookingEntity(eventId, userId);
        _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(userId)).ReturnsAsync(AppConstants.MaxActiveBookings - 1);
        _repository.Setup(repository => repository.CreateBookingAsync(eventId, userId)).ReturnsAsync(booking);

        var result = await _service.CreateBookingAsync(eventId, userId);

        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        _repository.Verify(repository => repository.CreateBookingAsync(eventId, userId), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_LimitReached_ThrowsException()
    {
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetCountOfActiveBookingsAsync(userId)).ReturnsAsync(AppConstants.MaxActiveBookings);

        await Assert.ThrowsAsync<ActiveBookingLimitException>(() => _service.CreateBookingAsync(eventId, userId));

        _repository.Verify(repository => repository.CreateBookingAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_PendingBooking_DoesNotPublishCancellation()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.CancelBookingAsync(booking.Id)).ReturnsAsync(true);

        await _service.CancelBookingAsync(booking.Id, booking.UserId, UserRole.User);

        _repository.Verify(repository => repository.CancelBookingAsync(booking.Id), Times.Once);
        _producer.Verify(producer => producer.PublishBookingCancelledMessageAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_ConfirmedBooking_PublishesCancellation()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.CancelBookingAsync(booking.Id)).ReturnsAsync(true);
        _producer.Setup(producer => producer.PublishBookingCancelledMessageAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.CancelBookingAsync(booking.Id, booking.UserId, UserRole.User);

        _producer.Verify(producer => producer.PublishBookingCancelledMessageAsync(
            It.Is<BookingCancelled>(message =>
                message.BookingId == booking.Id &&
                message.EventId == booking.EventId &&
                message.UserId == booking.UserId &&
                message.AmountSeats == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_AdminCancelsBooking_MessageContainsOwnerId()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        var adminId = Guid.NewGuid();
        booking.Confirm();
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.CancelBookingAsync(booking.Id)).ReturnsAsync(true);
        _producer.Setup(producer => producer.PublishBookingCancelledMessageAsync(It.IsAny<BookingCancelled>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.CancelBookingAsync(booking.Id, adminId, UserRole.Admin);

        _producer.Verify(producer => producer.PublishBookingCancelledMessageAsync(
            It.Is<BookingCancelled>(message => message.UserId == booking.UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelBookingAsync_OtherUsersBooking_ThrowsAccessDenied()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        var requestingUserId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        await Assert.ThrowsAsync<AccessDeniedException>(() =>
            _service.CancelBookingAsync(booking.Id, requestingUserId, UserRole.User));

        _repository.Verify(repository => repository.CancelBookingAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelBookingAsync_MissingBooking_ThrowsException()
    {
        var bookingId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetBookingByIdAsync(bookingId)).ReturnsAsync((BookingEntity?)null);

        await Assert.ThrowsAsync<BookingNotFoundException>(() =>
            _service.CancelBookingAsync(bookingId, Guid.NewGuid(), UserRole.User));
    }
}
