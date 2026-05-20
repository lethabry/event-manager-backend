using System.Net;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;
using FluentAssertions;

namespace EventManager.Tests.Services;

public class BookingRepositoryTests
{
    private readonly BookingRepository _repository;

    public BookingRepositoryTests()
    {
        _repository = new BookingRepository();
    }

    [Fact]
    [Trait("GetBookings", "Success")]
    public async Task GetBookings_NoStatusFilter_ShouldReturnAllBookings()
    {
        // Arrange
        var firstEventId = Guid.NewGuid();
        var secondEventId = Guid.NewGuid();
        var firstBooking = await _repository.CreateBookingAsync(firstEventId);
        var secondBooking = await _repository.CreateBookingAsync(secondEventId);

        // Act
        var result = await _repository.GetBookings();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(b => b.Id == firstBooking.Id);
        result.Should().Contain(b => b.Id == secondBooking.Id);
    }

    [Fact]
    [Trait("GetBookings", "Success")]
    public async Task GetBookings_WithStatusFilter_ShouldReturnMatchedBookings()
    {
        // Arrange
        var firstEvent = Guid.NewGuid();
        var secondEvent = Guid.NewGuid();
        var thirdEvent = Guid.NewGuid();
        await _repository.CreateBookingAsync(firstEvent);
        var secondBooking = await _repository.CreateBookingAsync(secondEvent);
        var thirdBooking = await _repository.CreateBookingAsync(thirdEvent);

        secondBooking.Confirm();
        thirdBooking.Reject();
        await _repository.UpdateBooking(secondBooking);
        await _repository.UpdateBooking(thirdBooking);

        // Act
        var pendingBookings = await _repository.GetBookings(BookingStatus.Pending);
        var confirmedBookings = await _repository.GetBookings(BookingStatus.Confirmed);
        var rejectedBookings = await _repository.GetBookings(BookingStatus.Rejected);

        // Assert
        pendingBookings.Should().HaveCount(1);
        confirmedBookings.Should().HaveCount(1);
        rejectedBookings.Should().HaveCount(1);
    }

    [Fact]
    [Trait("GetBookings", "Success")]
    public async Task GetBookings_WithStatusFilter_ShouldReturnEmptyList()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        await _repository.CreateBookingAsync(eventId);

        // Act
        var result = await _repository.GetBookings(BookingStatus.Rejected);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("GetBookings", "Success")]
    public async Task GetBookings_WhenRepositoryIsEmpty_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetBookings();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_IdExist_ShouldReturnBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var createdBooking = await _repository.CreateBookingAsync(eventId);

        // Act
        var result = await _repository.GetBookingByIdAsync(createdBooking.Id);

        // Assert
        result.Should().BeEquivalentTo(createdBooking);
        result.Id.Should().Be(createdBooking.Id);
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be(BookingStatus.Pending);
        result.ProcessedAt.Should().BeNull();
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_IdNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetBookingByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_AfterUpdate_ShouldReturnUpdatedBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var booking = await _repository.CreateBookingAsync(eventId);
        booking.Confirm();
        await _repository.UpdateBooking(booking);

        // Act
        var result = await _repository.GetBookingByIdAsync(booking.Id);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.ProcessedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("CreateBooking", "Success")]
    public async Task CreateBooking_ShouldCreateAndReturnBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var result = await _repository.CreateBookingAsync(eventId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.EventId.Should().Be(eventId);
        result.Status.Should().Be(BookingStatus.Pending);
        result.ProcessedAt.Should().BeNull();
    }

    [Fact]
    [Trait("UpdateBooking", "Success")]
    public async Task UpdateBooking_ShouldReturnUpdatedBooking()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var createdBooking = await _repository.CreateBookingAsync(eventId);
        createdBooking?.Confirm();

        // Act
        var result = await _repository.UpdateBooking(createdBooking);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    [Trait("UpdateBooking", "Error")]
    public async Task UpdateBooking_BookingNotExist_ShouldThrowBookingException()
    {
        // Arrange
        var nonExistingBooking = new Booking(Guid.NewGuid());

        // Act
        var result = () => _repository.UpdateBooking(nonExistingBooking);

        // Assert
        result.Should()
              .ThrowAsync<BookingException>()
              .WithMessage($"Мероприятия с id {nonExistingBooking.Id} не найдено")
              .Where((b) => b.statusCode == HttpStatusCode.NotFound);
    }
}