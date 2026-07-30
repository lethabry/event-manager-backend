using EventManager.Domain.Models;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class EventTests
{
    [Fact]
    public void ReleaseSeats_AfterReservation_ShouldRestoreAvailableSeats()
    {
        var evt = Event.Create(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            5);

        evt.TryReserveSeats();
        evt.ReleaseSeats();

        evt.AvailableSeats.Should().Be(5);
    }

    [Fact]
    public void TryReserveSeats_AfterRelease_ShouldReserveAgain()
    {
        var evt = Event.Create(
            "Test Event",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            1);

        var firstReservation = evt.TryReserveSeats();
        var reservationWithoutSeats = evt.TryReserveSeats();
        evt.ReleaseSeats();
        var reservationAfterRelease = evt.TryReserveSeats();

        firstReservation.Should().BeTrue();
        reservationWithoutSeats.Should().BeFalse();
        reservationAfterRelease.Should().BeTrue();
        evt.AvailableSeats.Should().Be(0);
    }
}
