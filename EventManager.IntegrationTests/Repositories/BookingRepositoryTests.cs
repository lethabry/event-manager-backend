using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories.BookingRepository;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests.Repositories;

public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
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

        return new AppDbContext(options);
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
        await using var repositoryContext = CreateContext();
        var repository = new BookingRepository(repositoryContext);
        var booking = await repository.CreateBookingAsync(evt1.Id);
        await using var verificationContext = CreateContext();
        var savedEvent = await verificationContext.Events.SingleAsync(e => e.Id == evt1.Id);
        var savedBookings = await verificationContext.Bookings.Where(b => b.EventId == evt1.Id).ToListAsync();

        //Assert
        Assert.NotNull(booking);
        Assert.Equal(evt1.Id, booking.EventId);
        Assert.Equal(4, savedEvent.AvailableSeats);
        Assert.Single(savedBookings);
        Assert.Equal(booking.Id, savedBookings[0].Id);
    }

    [Fact]
    public async Task CreateBooking_NotExistingEvent_ThrowException()
    {
        //Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var eventId = Guid.NewGuid();

        //Act && Assert
        var exception = await Assert.ThrowsAsync<EventNotFoundException>(
            () => repository.CreateBookingAsync(eventId));
        Assert.Equal(eventId, exception.EventId);
    }
    
    [Theory]
    [InlineData(1, 20)]
    [InlineData(5, 20)]
    public async Task CreateBooking_ConcurrentContexts_ShouldNotOverbook(
        int totalSeats,
        int requestCount)
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        Guid eventId;

        await using (var seedContext = CreateContext())
        {
            var evt = Event.Create("Concurrent Event", now, now.AddDays(1), totalSeats);
            eventId = evt.Id;
            await seedContext.Events.AddAsync(evt);
            await seedContext.SaveChangesAsync();
        }

        //Act
        var startGate = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, requestCount)
            .Select(async _ =>
            {
                await startGate.Task;
                await using var context = CreateContext();
                var repository = new BookingRepository(context);

                try
                {
                    var booking = await repository.CreateBookingAsync(eventId);
                    return new BookingAttempt(booking, null);
                }
                catch (NoAvailableSeatsException exception)
                {
                    return new BookingAttempt(null, exception);
                }
            })
            .ToArray();

        startGate.SetResult(true);
        var attempts = await Task.WhenAll(tasks);

        var successfulBookings = attempts
            .Where(attempt => attempt.Booking != null)
            .Select(attempt => attempt.Booking!)
            .ToList();
        var conflicts = attempts
            .Where(attempt => attempt.Conflict != null)
            .Select(attempt => attempt.Conflict!)
            .ToList();

        //Arrange
        Assert.Equal(totalSeats, successfulBookings.Count);
        Assert.Equal(requestCount - totalSeats, conflicts.Count);
        Assert.All(conflicts, exception => Assert.Equal(eventId, exception.EventId));
        Assert.Equal(
            successfulBookings.Count,
            successfulBookings.Select(booking => booking.Id).Distinct().Count());

        await using var verificationContext = CreateContext();
        var savedEvent = await verificationContext.Events.SingleAsync(e => e.Id == eventId);
        var savedBookings = await verificationContext.Bookings
            .Where(booking => booking.EventId == eventId)
            .ToListAsync();

        Assert.Equal(0, savedEvent.AvailableSeats);
        Assert.Equal(totalSeats, savedBookings.Count);
        Assert.Equal(
            savedBookings.Count,
            savedBookings.Select(booking => booking.Id).Distinct().Count());
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
        booking.Confirm();
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
        await Assert.ThrowsAsync<BookingNotFoundException>(() => repository.UpdateBookingAsync(booking));
    }

    private sealed record BookingAttempt(
        Booking? Booking,
        NoAvailableSeatsException? Conflict);
}
