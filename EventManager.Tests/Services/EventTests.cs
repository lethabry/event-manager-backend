using EventManager.Domain.Models;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class EventTests
{
    [Fact]
    public void ReleaseSeats_AfterReservation_ShouldRestoreAvailableSeats()
    {
        //Arrange
        var evt = Event.Create(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            5);

        //Act
        evt.TryReserveSeats();
        evt.ReleaseSeats();

        //Assert
        evt.AvailableSeats.Should().Be(5);
    }

    [Fact]
    public void TryReserveSeats_AfterRelease_ShouldReserveAgain()
    {
        //Arrange
        var evt = Event.Create(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            1);

        //Act
        var firstReservation = evt.TryReserveSeats();
        var reservationWithoutSeats = evt.TryReserveSeats();
        evt.ReleaseSeats();
        var reservationAfterRelease = evt.TryReserveSeats();

        //Assert
        firstReservation.Should().BeTrue();
        reservationWithoutSeats.Should().BeFalse();
        reservationAfterRelease.Should().BeTrue();
        evt.AvailableSeats.Should().Be(0);
    }
}
