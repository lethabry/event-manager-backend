using Booking.Application.Interfaces;
using Booking.Application.Services.BookingMessagesProcess;
using Booking.Domain.Common;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.Logging;
using Moq;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.UnitTests.Services;

public class BookingMessagesProcessTests
{
    private readonly Mock<IBookingRepository> _repository = new();
    private readonly BookingMessagesProcess _processor;

    public BookingMessagesProcessTests()
    {
        _processor = new BookingMessagesProcess(
            Mock.Of<ILogger<BookingMessagesProcess>>(),
            _repository.Object);
    }

    [Fact]
    public async Task HandleProcessAsync_ConfirmedBooking_RejectsAndSavesBooking()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        var message = CreateMessage(booking);
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        _repository.Verify(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleProcessAsync_CancelledBooking_DoesNotUpdateBooking()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        booking.Cancel();
        var message = CreateMessage(booking);
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        _repository.Verify(repository => repository.UpdateBookingAsync(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleProcessAsync_MissingBooking_DoesNotUpdateBooking()
    {
        var message = new BookingRejected
        {
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AmountSeats = 1
        };
        _repository.Setup(repository => repository.GetBookingByIdAsync(message.BookingId)).ReturnsAsync((BookingEntity?)null);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        _repository.Verify(repository => repository.UpdateBookingAsync(It.IsAny<BookingEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleProcessAsync_SaveFails_PropagatesException()
    {
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        var message = CreateMessage(booking);
        _repository.Setup(repository => repository.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);
        _repository.Setup(repository => repository.UpdateBookingAsync(booking, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.HandleProcessAsync(message, CancellationToken.None));
    }

    private static BookingRejected CreateMessage(BookingEntity booking)
    {
        return new BookingRejected
        {
            BookingId = booking.Id,
            EventId = booking.EventId,
            UserId = booking.UserId,
            AmountSeats = 1
        };
    }
}
