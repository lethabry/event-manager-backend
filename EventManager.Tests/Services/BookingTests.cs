using EventManager.Domain.Common;
using EventManager.Domain.Models;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class BookingTests
{
    [Fact]
    public void initBooking_ShouldReturnPendingBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();

        //Act
        var booking = new Booking(eventId);

        //Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ProcessedAt.Should().BeNull();
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void confirmBooking_ShouldReturnConfirmedBooking()
    {
        //Arrange
        var eventId = Guid.NewGuid();

        //Act
        var booking = new Booking(eventId);
        var status = booking.Confirm();

        //Assert
        status.Should().Be(true);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
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

        //Act
        var booking = new Booking(eventId);
        var status = booking.Reject();

        //Assert
        status.Should().Be(true);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
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

        //Act
        var booking = new Booking(eventId);
        booking.Reject();
        var status = booking.Confirm();

        //Assert
        status.Should().Be(false);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
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

        //Act
        var booking = new Booking(eventId);
        booking.Confirm();
        var status = booking.Reject();

        //Assert
        status.Should().Be(false);
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
