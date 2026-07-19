using System.Net;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Data.DataAccess;
using EventManager.Exceptions;
using EventManager.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Tests.Services;

public class BookingRepositoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly IBookingRepository _repository;

    public BookingRepositoryTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IBookingRepository, BookingRepository>();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _repository = _scope.ServiceProvider.GetRequiredService<IBookingRepository>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
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
        var result = await _repository.GetBookingsAsync();

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
        await _repository.UpdateBookingAsync(secondBooking);
        await _repository.UpdateBookingAsync(thirdBooking);

        // Act
        var pendingBookings = await _repository.GetBookingsAsync(BookingStatus.Pending);
        var confirmedBookings = await _repository.GetBookingsAsync(BookingStatus.Confirmed);
        var rejectedBookings = await _repository.GetBookingsAsync(BookingStatus.Rejected);

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
        var result = await _repository.GetBookingsAsync(BookingStatus.Rejected);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("GetBookings", "Success")]
    public async Task GetBookings_WhenRepositoryIsEmpty_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetBookingsAsync();

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
        await _repository.UpdateBookingAsync(booking);

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
        var result = await _repository.UpdateBookingAsync(createdBooking);

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
        var result = () => _repository.UpdateBookingAsync(nonExistingBooking);

        // Assert
        await result.Should()
            .ThrowAsync<BookingException>()
            .WithMessage($"Бронирование с id {nonExistingBooking.Id} не найдено")
            .Where((b) => b.statusCode == HttpStatusCode.NotFound);
    }
}
