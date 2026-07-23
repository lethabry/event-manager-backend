using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Data.DataAccess;
using EventManager.Exceptions;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
namespace EventManager.IntegrationTests.Repositories;

public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }

    [Fact]
    public async Task CreateBooking_ExistingEvent_CreteBooking()
    {
        //Arrange
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act
        var repository = new BookingRepository(CreateContext());
        var booking = await repository.CreateBookingAsync(evt1.Id);

        //Assert
        Assert.NotNull(booking);
        Assert.Equal(evt1.Id, booking.EventId);
    }

    [Fact]
    public async Task CreateBooking_NotExistingEvent_ThrowException()
    {
        //Arrange
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act && Assert
        var repository = new BookingRepository(CreateContext());
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetBookings_ReturnsBookings()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);
        await context.Bookings.AddAsync(new Booking(evt1.Id));
        await context.Bookings.AddAsync(new Booking(evt2.Id));
        await context.SaveChangesAsync();

        //Act
        var repository = new BookingRepository(CreateContext());
        var bookings = await repository.GetBookingsAsync();

        //Assert
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Count);
    }

    [Fact]
    public async Task GetBookings_BookingsNotExist_ReturnsEmptyList()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);

        //Act
        var repository = new BookingRepository(CreateContext());
        var bookings = await repository.GetBookingsAsync();

        //Assert
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task GetBookings_StatusFilter_ReturnFilteredBookings()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);

        var booking1 = new Booking(evt1.Id);
        booking1.Confirm();
        var booking2 = new Booking(evt2.Id);
        await context.Bookings.AddRangeAsync([booking1, booking2]);
        await context.SaveChangesAsync();

        //Act
        var repository = new BookingRepository(CreateContext());
        var confirmedBookings = await repository.GetBookingsAsync(BookingStatus.Confirmed);
        var pendingBookings = await repository.GetBookingsAsync(BookingStatus.Pending);
        var rejectedBookings = await repository.GetBookingsAsync(BookingStatus.Rejected);

        //Assert
        Assert.Single(confirmedBookings);
        Assert.Single(pendingBookings);
        Assert.Empty(rejectedBookings);
    }

    [Fact]
    public async Task GetBookingById_BookingExist_ReturnsBooking()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync(evt1);

        var booking1 = new Booking(evt1.Id);
        await context.Bookings.AddAsync(booking1);
        await context.SaveChangesAsync();

        //Act
        var repository = new BookingRepository(CreateContext());
        var booking = await repository.GetBookingByIdAsync(booking1.Id);

        //Assert
        Assert.NotNull(booking);
        Assert.Equal(evt1.Id, booking.EventId);
        Assert.Equal(booking.Id, booking1.Id);
    }

    [Fact]
    public async Task GetBookingById_BookingNotExist_ReturnsNull()
    {
        //Act
        var repository = new BookingRepository(CreateContext());
        var booking = await repository.GetBookingByIdAsync(Guid.NewGuid());

        //Assert
        Assert.Null(booking);
    }

    [Fact]
    public async Task UpdateBooking_ReturnBooking()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync(evt1);

        var booking1 = new Booking(evt1.Id);
        await context.Bookings.AddAsync(booking1);
        await context.SaveChangesAsync();

        //Act
        var repository = new BookingRepository(CreateContext());
        var booking = await repository.GetBookingByIdAsync(booking1.Id);
        booking?.Confirm();
        var updateBooking = await repository.UpdateBookingAsync(booking);

        //Assert
        Assert.NotNull(booking);
        Assert.NotNull(updateBooking);
        Assert.Equal(BookingStatus.Confirmed, updateBooking.Status);
    }

    [Fact]
    public async Task UpdateBooking_BookingNotExist_ReturnBooking()
    {
        //Act && Arrange
        var booking = new Booking(Guid.NewGuid());
        var repository = new BookingRepository(CreateContext());
        await Assert.ThrowsAsync<BookingException>(() => repository.UpdateBookingAsync(booking));
    }

}
