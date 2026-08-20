using Booking.Domain.Common;
using BookingEntity = Booking.Domain.Models.Booking;
using FluentAssertions;

namespace Booking.UnitTests.Services;

public class BookingTests
{
    [Fact]
    public void initBooking_ShouldReturnPendingBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //Act
        var booking = new BookingEntity(eventId, userId);

        //Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.EventId.Should().Be(eventId);
        booking.UserId.Should().Be(userId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void confirmBooking_ShouldReturnConfirmedBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //Act
        var booking = new BookingEntity(eventId, userId);
        var status = booking.Confirm();

        //Assert
        status.Should().Be(true);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.UserId.Should().Be(userId);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void rejectBooking_ShouldReturnRejectedBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //Act
        var booking = new BookingEntity(eventId, userId);
        var status = booking.Reject();

        //Assert
        status.Should().Be(true);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.UserId.Should().Be(userId);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void confirmRejectedBooking_ShouldNotChangeStatus()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //Act
        var booking = new BookingEntity(eventId, userId);
        booking.Reject();
        var status = booking.Confirm();

        //Assert
        status.Should().Be(false);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.UserId.Should().Be(userId);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void rejectConfirmedBooking_ShouldNotChangeStatus()
    {
        //Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        //Act
        var booking = new BookingEntity(eventId, userId);
        booking.Confirm();
        var status = booking.Reject();

        //Assert
        status.Should().Be(false);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.UserId.Should().Be(userId);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
