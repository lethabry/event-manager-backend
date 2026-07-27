using EventManager.Application.DTOs;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories.EventRepository;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests.Repositories;

public class EventRepositoryTests : IAsyncLifetime
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
    public async Task CreateEvent_SavesEventToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var evtDTO = new CreateEventDTO()
        {
            Title = "Премьера: 'Дюна: Часть вторая' (IMAX)",
            Description = "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.",
            EndAt = DateTime.UtcNow.AddDays(1),
            StartAt = DateTime.UtcNow.AddDays(-1),
            TotalSeats = 5,
        };
        // Act
        var savedEvt = await repository.CreateEventAsync(evtDTO);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == savedEvt.Id);

        Assert.NotNull(saved);
        Assert.Equal(evtDTO.Title, saved.Title);
        Assert.Equal(evtDTO.Description, saved.Description);
        Assert.Equal(evtDTO.TotalSeats, saved.TotalSeats);
    }

    [Fact]
    public async Task CreateEvent_CheckMaxLengthTitle_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act && Assert
        var evtDTO = new CreateEventDTO()
        {
            Title =
                "Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)Премьера: 'Дюна: Часть вторая' (IMAX)",
            Description = "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.",
            EndAt = DateTime.UtcNow.AddDays(1),
            StartAt = DateTime.UtcNow.AddDays(-1),
            TotalSeats = 5,
        };

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.CreateEventAsync(evtDTO));
    }

    [Fact]
    public async Task CreateEvent_CheckMaxLengthDescription_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        // Act && Assert
        var evtDTO = new CreateEventDTO()
        {
            Title =
                "Премьера: 'Дюна: Часть вторая'",
            Description =
                "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами.",
            EndAt = DateTime.UtcNow.AddDays(1),
            StartAt = DateTime.UtcNow.AddDays(-1),
            TotalSeats = 5,
        };

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.CreateEventAsync(evtDTO));
    }

    [Fact]
    public async Task GetEvents_WithoutFilters_GetAllEvents()
    {
        //Arrange 
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync(null, null, null);

        //Assert
        Assert.NotNull(events);
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task GetEvents_WithTitleFilter_GetFilteredEvents()
    {
        //Arrange 
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now, now.AddDays(1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync("new", null, null);

        //Assert
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(evt2.Title, events.First().Title);
    }

    [Fact]
    public async Task GetEvents_WithDataFromFilter_GetFilteredEvents()
    {
        //Arrange 
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync(null, now.AddDays(-1), null);

        //Assert
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(evt1.Title, events.First().Title);
    }

    [Fact]
    public async Task GetEvents_WithDataToFilter_GetFilteredEvents()
    {
        //Arrange 
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync(null, null, now);

        //Assert
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(evt2.Title, events.First().Title);
    }

    [Fact]
    public async Task GetEvents_CombinedFilters_GetFilteredEvents()
    {
        //Arrange
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("Test name", now.AddDays(2), now.AddDays(3), 5);
        var evt3 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2, evt3]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync("test", now, now.AddDays(1));

        //Assert
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal(evt1.Title, events.First().Title);
    }

    [Fact]
    public async Task GetEvents_InvalidFilters_GetFilteredEvents()
    {
        //Arrange
        var now = DateTime.UtcNow;
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("Test name", now.AddDays(2), now.AddDays(3), 5);
        var evt3 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2, evt3]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync(null, now, now.AddDays(-1));

        //Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEvents_EmptyDatabase_GetEmptyList()
    {
        //Arrange
        await ResetDatabaseAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var events = await repository.GetEventsAsync(null, null, null);

        //Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventById_EmptyDatabase_GetNull()
    {
        //Arrange
        await ResetDatabaseAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var evt = await repository.GetEventByIdAsync(Guid.NewGuid());

        //Assert
        Assert.Null(evt);
    }

    [Fact]
    public async Task GetEventById_NotExistEventId_GetNull()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("Test name", now.AddDays(2), now.AddDays(3), 5);
        var evt3 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2, evt3]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var evt = await repository.GetEventByIdAsync(Guid.NewGuid());

        //Assert
        Assert.Null(evt);
    }

    [Fact]
    public async Task GetEventById_ExistEventId_GetEvent()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        var evt2 = Event.Create("Test name", now.AddDays(2), now.AddDays(3), 5);
        var evt3 = Event.Create("New Event", now.AddDays(-2), now.AddDays(-1), 5);
        await context.Events.AddRangeAsync([evt1, evt2, evt3]);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var e1 = await repository.GetEventByIdAsync(evt1.Id);
        var e2 = await repository.GetEventByIdAsync(evt2.Id);
        var e3 = await repository.GetEventByIdAsync(evt3.Id);

        //Assert
        Assert.NotNull(e1);
        Assert.NotNull(e2);
        Assert.NotNull(e3);
        Assert.Equal(e1.Title, evt1.Title);
        Assert.Equal(e2.Title, evt2.Title);
        Assert.Equal(e3.Title, evt3.Title);
    }

    [Fact]
    public async Task UpdateEvent_NotExistEventId_GetNull()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var evtInfo = new EventInfoDTO()
        {
            Title = "Test Event",
            Description = "Test Description",
            AvailableSeats = 5,
            StartAt = now,
            EndAt = now.AddDays(1),
        };
        var updatedEvt = await repository.UpdateEventAsync(Guid.NewGuid(), evtInfo);

        //Assert
        Assert.Null(updatedEvt);
    }

    [Fact]
    public async Task UpdateEvent_ExistingEvent_GetUpdatedEvent()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var evtInfo = new EventInfoDTO()
        {
            Title = "New ",
            Description = "Test Description",
            AvailableSeats = 5,
            StartAt = now.AddDays(1),
            EndAt = now.AddDays(2),
        };
        var updatedEvt = await repository.UpdateEventAsync(evt1.Id, evtInfo);

        //Assert
        Assert.NotNull(updatedEvt);
        Assert.Equal(updatedEvt.Title, evtInfo.Title);
        Assert.Equal(updatedEvt.Description, evtInfo.Description);
        Assert.Equal(updatedEvt.AvailableSeats, evtInfo.AvailableSeats);
    }

    [Fact]
    public async Task DeleteEvent_NotExistEventId_ReturnFalse()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.DeleteEventAsync(Guid.NewGuid());

        //Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEvent_ExistEventId_ReturnTrue()
    {
        //Arrange
        await ResetDatabaseAsync();
        var now = DateTime.UtcNow;
        await using var context = CreateContext();
        var evt1 = Event.Create("Test Event", now, now.AddDays(1), 5);
        await context.Events.AddAsync(evt1);
        await context.SaveChangesAsync();

        //Act
        var repository = new EventRepository(CreateContext());
        var result = await repository.DeleteEventAsync(evt1.Id);

        //Assert
        Assert.True(result);
    }
}
