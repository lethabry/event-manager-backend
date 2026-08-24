using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using Booking.Infrastructure.DataAccess;
using Booking.Infrastructure.Repositories.BookingRepository;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.IntegrationTests.Repositories;

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

    [Fact]
    public async Task CreateBookingAsync_PersistsPendingBooking()
    {
        await ResetDatabaseAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var booking = await repository.CreateBookingAsync(eventId, userId);

        await using var verificationContext = CreateContext();
        var savedBooking = await verificationContext.Bookings.SingleAsync(entity => entity.Id == booking.Id);
        Assert.Equal(eventId, savedBooking.EventId);
        Assert.Equal(userId, savedBooking.UserId);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
    {
        await ResetDatabaseAsync();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var seedContext = CreateContext();
        var seedRepository = new BookingRepository(seedContext);
        var created = await seedRepository.CreateBookingAsync(eventId, userId);
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var booking = await repository.GetBookingByIdAsync(created.Id);

        Assert.NotNull(booking);
        Assert.Equal(created.Id, booking.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_MissingBooking_ReturnsNull()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var booking = await repository.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(booking);
    }

    [Fact]
    public async Task GetBookingsAsync_StatusFilter_ReturnsMatchingBookings()
    {
        await ResetDatabaseAsync();
        var confirmed = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        confirmed.Confirm();
        var pending = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        await using (var seedContext = CreateContext())
        {
            await seedContext.Bookings.AddRangeAsync(confirmed, pending);
            await seedContext.SaveChangesAsync();
        }
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var confirmedBookings = await repository.GetBookingsAsync(BookingStatus.Confirmed);
        var pendingBookings = await repository.GetBookingsAsync(BookingStatus.Pending);
        var rejectedBookings = await repository.GetBookingsAsync(BookingStatus.Rejected);

        Assert.Single(confirmedBookings);
        Assert.Single(pendingBookings);
        Assert.Empty(rejectedBookings);
    }

    [Fact]
    public async Task UpdateBookingAsync_ConfirmedBooking_PersistsStatus()
    {
        await ResetDatabaseAsync();
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        await using (var seedContext = CreateContext())
        {
            await seedContext.Bookings.AddAsync(booking);
            await seedContext.SaveChangesAsync();
        }
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var storedBooking = await repository.GetBookingByIdAsync(booking.Id);
        Assert.NotNull(storedBooking);
        storedBooking.Confirm();

        await repository.UpdateBookingAsync(storedBooking);

        await using var verificationContext = CreateContext();
        var updatedBooking = await verificationContext.Bookings.SingleAsync(entity => entity.Id == booking.Id);
        Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
        Assert.NotNull(updatedBooking.ProcessedAt);
    }

    [Fact]
    public async Task UpdateBookingAsync_MissingBooking_ThrowsException()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<BookingNotFoundException>(() => repository.UpdateBookingAsync(booking));
    }

    [Fact]
    public async Task CancelBookingAsync_ConfirmedBooking_PersistsCancellation()
    {
        await ResetDatabaseAsync();
        var booking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        booking.Confirm();
        await using (var seedContext = CreateContext())
        {
            await seedContext.Bookings.AddAsync(booking);
            await seedContext.SaveChangesAsync();
        }
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var result = await repository.CancelBookingAsync(booking.Id);

        await using var verificationContext = CreateContext();
        var cancelledBooking = await verificationContext.Bookings.SingleAsync(entity => entity.Id == booking.Id);
        Assert.True(result);
        Assert.Equal(BookingStatus.Cancelled, cancelledBooking.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_MissingBooking_ThrowsException()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        await Assert.ThrowsAsync<BookingNotFoundException>(() => repository.CancelBookingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCountOfActiveBookingsAsync_CountsOnlyPendingAndConfirmedForUser()
    {
        await ResetDatabaseAsync();
        var userId = Guid.NewGuid();
        var pending = new BookingEntity(Guid.NewGuid(), userId);
        var confirmed = new BookingEntity(Guid.NewGuid(), userId);
        confirmed.Confirm();
        var cancelled = new BookingEntity(Guid.NewGuid(), userId);
        cancelled.Cancel();
        var anotherUsersBooking = new BookingEntity(Guid.NewGuid(), Guid.NewGuid());
        await using (var seedContext = CreateContext())
        {
            await seedContext.Bookings.AddRangeAsync(pending, confirmed, cancelled, anotherUsersBooking);
            await seedContext.SaveChangesAsync();
        }
        await using var context = CreateContext();
        var repository = new BookingRepository(context);

        var count = await repository.GetCountOfActiveBookingsAsync(userId);

        Assert.Equal(2, count);
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
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE bookings RESTART IDENTITY CASCADE");
    }
}
