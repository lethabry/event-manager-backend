using Event.Application.DTOs;
using Event.Application.Interfaces;
using Event.Application.Services.EventService;
using Event.Application.Services.EventValidatorService;
using Event.Domain.Exceptions;
using Event.Domain.Models;
using EventEntity = Event.Domain.Models.Event;
using Event.Infrastructure.DataAccess;
using Event.Infrastructure.Repositories.EventRepository;
using EventManager.Contracts.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Event.UnitTests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly Mock<IEventValidatorService> _mockValidationService;
    private readonly IEventService _eventService;
    private readonly AppDbContext _dbContext;
    private readonly List<EventEntity> _events;

    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    public EventServiceTests()
    {
        _events =
        [
            EventEntity.Create("Премьера: 'Дюна: Часть вторая' (IMAX)", new DateTime(2026, 4, 22, 19, 0, 0), new DateTime(2026, 4, 22, 22, 15, 0), 50, "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами."),
            EventEntity.Create("Ночь в кино: Трилогия 'Назад в будущее'", new DateTime(2026, 4, 25, 23, 0, 0), new DateTime(2026, 4, 26, 5, 0, 0), 100, "Марафон всех трех частей с перерывом на пиццу. Начало в 23:00. Вход 500₽."),
            EventEntity.Create("Опера 'Кармен' (Новая сцена)", new DateTime(2026, 5, 12, 19, 0, 0), new DateTime(2026, 5, 12, 22, 30, 0), 150, "Дирижер — приглашенный маэстро из Ла Скала. Дресс-код: вечерний."),
            EventEntity.Create("Закрытый показ: 'Мастер и Маргарита' (режиссерская версия)", new DateTime(2026, 5, 14, 20, 0, 0), new DateTime(2026, 5, 14, 23, 0, 0), 200, "Только для членов клуба. После показа — Q&A с режиссером."),
        ];
        _mockValidationService = new Mock<IEventValidatorService>();

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventValidatorService>(_ => _mockValidationService.Object);

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();

        _dbContext.Events.AddRange(_events);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    public static IEnumerable<object[]> DatesTestData()
    {
        return
        [
            [new DateTime(2026, 4, 12), DateTime.Now],
            [new DateTime(2026, 4, 12), new DateTime(2026, 4, 13)],
            [new DateTime(2026, 5, 1), new DateTime(2026, 5, 22)]
        ];
    }

    public static IEnumerable<object[]> FiltersTestData()
    {
        return
        [
            ["Опера", new DateTime(2026, 4, 12), new DateTime(2026, 4, 13)],
            ["п", new DateTime(2026, 4, 1), DateTime.Now]
        ];
    }

    public static IEnumerable<object[]> PaginationAndFiltersTestData()
    {
        return
        [
            ["Опера", new DateTime(2026, 4, 12), new DateTime(2026, 4, 13), 1, 2],
            ["п", new DateTime(2026, 4, 1), DateTime.Now, 2, 2]
        ];
    }

    [Fact]
    [Trait("GetEvents", "Success")]
    public async Task GetEvents_NoFilters_ShouldReturnAllPaginatedEvents()
    {
        //Arrange
        var padinatedEvents = _events.Take(DefaultPageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = DefaultPage,
            CurrentPageSize = DefaultPageSize,
            Result = padinatedEvents,
            TotalAmount = _events.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(null, null, null, 1, 10);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData("Опера")]
    [InlineData("П")]
    [InlineData("IMAX")]
    [Trait("GetEvents", "Success")]
    public async Task GetEvents_FilterByTitle_ShouldReturnMatchingEvents(string titleValue)
    {
        //Arrange
        var evt = _events.Where((e) => e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var paginatedEvents = evt.Take(DefaultPageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = DefaultPage,
            CurrentPageSize = DefaultPageSize,
            Result = paginatedEvents,
            TotalAmount = evt.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(titleValue, null, null, 1, 10);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData("asdasd")]
    [InlineData("123")]
    [InlineData("321")]
    [Trait("GetEvents", "Success")]
    public async Task GetEvents_FilterByTitle_ShouldReturnEmptyList(string titleValue)
    {
        //Arrange
        var evt = _events.Where((e) => e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var paginatedEvents = evt.Take(DefaultPageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = DefaultPage,
            CurrentPageSize = DefaultPageSize,
            Result = paginatedEvents,
            TotalAmount = evt.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(titleValue, null, null, 1, 10);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(DatesTestData))]
    [Trait("GetEvents", "Success")]
    public async Task GetEvents_FilterByDates_ShouldReturnMatchingEvents(DateTime fromDate, DateTime toDate)
    {
        //Arrange
        var evts = _events.Where((e) => e.StartAt >= fromDate && e.EndAt <= toDate)
            .ToList();
        var paginatedEvents = evts.Take(DefaultPageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = DefaultPage,
            CurrentPageSize = DefaultPageSize,
            Result = paginatedEvents,
            TotalAmount = evts.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(null, fromDate, toDate, 1, 10);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(FiltersTestData))]
    [Trait("GetEvents", "Success")]
    public async Task GetEvent_AllFilters_ShouldReturnMatchingEvents(string titleValue, DateTime fromDate, DateTime toDate)
    {
        //Arrange
        var evts = _events.Where((e) =>
                e.StartAt >= fromDate && e.EndAt <= toDate &&
                e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var paginatedEvents = evts.Take(DefaultPageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = DefaultPage,
            CurrentPageSize = DefaultPageSize,
            Result = paginatedEvents,
            TotalAmount = evts.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(titleValue, fromDate, toDate, 1, 10);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(4, 1)]
    [Trait("GetEvents", "Success")]
    public async Task GetEvent_CustomPaginationWithoutFilters_ShouldReturnMatchingEvents(int page, int pageSize)
    {
        //Arrange
        var evts = _events.Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = evts,
            TotalAmount = _events.Count
        };

        //Act
        var result = await _eventService.GetEventsAsync(null, null, null, page, pageSize);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(PaginationAndFiltersTestData))]
    [Trait("GetEvents", "Success")]
    public async Task GetEvent_CustomPaginationWitFilters_ShouldReturnMatchingEvents(string titleValue, DateTime fromDate,
        DateTime toDate, int page, int pageSize)
    {
        //Arrange
        var evts = _events.Where((e) =>
            e.StartAt >= fromDate && e.EndAt <= toDate &&
            e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase));
        var paginatedEvents = evts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var paginatedResult = new PaginatedResultDTO<EventEntity>()
        {
            CurrentPage = page,
            CurrentPageSize = pageSize,
            Result = paginatedEvents,
            TotalAmount = evts.Count()
        };

        //Act
        var result = await _eventService.GetEventsAsync(titleValue, fromDate, toDate, page, pageSize);

        //Assert
        result.Should()
            .BeEquivalentTo(paginatedResult);
    }

    [Fact]
    [Trait("GetEvents", "Exception")]
    public async Task GetEvents_FromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var toDate = DateTime.Now.AddDays(-100);
        var fromDate = DateTime.Now;

        _mockValidationService.Setup((validation) => validation.ValidatePaginatedResult(fromDate, toDate, 1, 10))
            .Throws(() => new EventValidationException("Дата начала мероприятия должны быть раньше даты окончания мероприятия"));

        //Act
        Func<Task> act = () => _eventService.GetEventsAsync(null, fromDate, toDate, 1, 10);

        //Assert
        await act.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата начала мероприятия должны быть раньше даты окончания мероприятия");
        _mockValidationService.Verify(x => x.ValidatePaginatedResult(fromDate, toDate, 1, 10), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("GetEvents", "Exception")]
    public async Task GetEvents_InvalidPage_ShouldThrowError(int page)
    {
        //Arrange
        _mockValidationService.Setup((validation) => validation.ValidatePaginatedResult(null, null, page, 10))
            .Throws(() => new EventValidationException("Номер страницы не может быть меньше 1"));

        //Act
        var result = () => _eventService.GetEventsAsync(null, null, null, page, 10);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Номер страницы не может быть меньше 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("GetEvents", "Exception")]
    public async Task GetEvents_InvalidPageSize_ShouldThrowError(int pageSize)
    {
        //Arrange
        _mockValidationService.Setup((validation) => validation.ValidatePaginatedResult(null, null, 1, pageSize))
            .Throws(() => new EventValidationException("Количество элементов не может быть меньше 1"));

        //Act
        var result = async () => await _eventService.GetEventsAsync(null, null, null, 1, pageSize);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество элементов не может быть меньше 1");
    }

    [Fact]
    [Trait("GetEventsById", "Success")]
    public async Task GetEventById_IdExist_ShouldReturnEvent()
    {
        //Arrange
        var evt = _events.First();

        //Act
        var result = await _eventService.GetEventByIdAsync(evt.Id);

        //Assert
        result.Should().BeEquivalentTo(evt);
    }

    [Fact]
    [Trait("GetEventsById", "Error")]
    public async Task GetEventById_IdNotExist_ShouldThrowError()
    {
        //Arrange
        var randomId = Guid.NewGuid();

        //Act
        var result = async () => await _eventService.GetEventByIdAsync(randomId);

        //Assert
        await result.Should()
                .ThrowAsync<EventNotFoundException>()
                .WithMessage($"Мероприятие с id {randomId} не найдено");
    }

    [Fact]
    [Trait("CreateEvent", "Success")]
    public async Task CreateEvent_ValidEventDTO_ShouldReturnCreatedEvent()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            Description = "Новое крутое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 100
        };

        var createdEvent = EventEntity.Create(evnt.Title, evnt.StartAt, evnt.EndAt, evnt.TotalSeats, evnt.Description);

        //Act
        var result = await _eventService.CreateEventAsync(evnt);

        //Assert
        result.Should()
            .BeEquivalentTo(createdEvent, options => options.Excluding(x => x.Id));
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Success")]
    public async Task CreateEvent_ValidEventDTOWithoutDescription_ShouldReturnCreatedEvent()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 100
        };

        var createdEvent = EventEntity.Create(evnt.Title, evnt.StartAt, evnt.EndAt, evnt.TotalSeats);

        //Act
        var result = await _eventService.CreateEventAsync(evnt);

        //Assert
        result.Should()
            .BeEquivalentTo(createdEvent, options => options.Excluding(x => x.Id));
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTOEmptyTitle_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 100
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(() =>
                new EventValidationException(
                    "Название мероприятия не может быть пустым"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Название мероприятия не может быть пустым");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTOFromDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 100
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата начала мероприятия должна быть заполнена"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата начала мероприятия должна быть заполнена");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTOToDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue,
            TotalSeats = 100
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата конца мероприятия должна быть заполнена"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата конца мероприятия должна быть заполнена");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTOFromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 100
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTOZeroTotalSeats_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 0
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество мест должно быть больше 0"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество мест должно быть больше 0");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public async Task CreateEvent_InvalidDTONegativeTotalSeats_ShouldThrowError()
    {
        //Arrange
        var evnt = new CreateEventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = -5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество мест должно быть больше 0"));

        //Act
        var result = async () => await _eventService.CreateEventAsync(evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество мест должно быть больше 0");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Success")]
    public async Task UpdateEvent_ValidDTOWithExistingId_ShouldReturnUpdatedEvent()
    {
        //Arrange
        var evnt = _events.First();
        var updateEventDTO = new EventInfoDTO()
        {
            Title = "Новое название",
            Description = "Новое описание",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 5,
        };
        var updateEvent = EventEntity.Reconstruct("Новое название", updateEventDTO.StartAt, updateEventDTO.EndAt, 5, 5, "Новое описание");

        //Act
        var result = await _eventService.UpdateEventAsync(evnt.Id, updateEventDTO);

        //Assert
        result.Should()
            .BeEquivalentTo(updateEvent, option => option.Excluding(x => x.Id));
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Success")]
    public async Task UpdateEvent_ValidDTOWithoutDescriptionWithExistingId_ShouldReturnUpdatedEvent()
    {
        //Arrange
        var evnt = _events.First();
        var updateEventDTO = new EventInfoDTO()
        {
            Title = "Новое название",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 5
        };
        var updateEvent = EventEntity.Reconstruct("Новое название", updateEventDTO.StartAt, updateEventDTO.EndAt, 5, 5);

        //Act
        var result = await _eventService.UpdateEventAsync(evnt.Id, updateEventDTO);

        //Assert
        result.Should()
            .BeEquivalentTo(updateEvent, option => option.Excluding(x => x.Id));
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_ValidDTOWithNotExistingId_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var updateEventDTO = new EventInfoDTO()
        {
            Title = "Новое название",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 5
        };

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, updateEventDTO);

        //Assert
        await result.Should()
                .ThrowAsync<EventNotFoundException>()
                .WithMessage($"Мероприятие с id {evntId} не найдено");
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOTitleIsEmpty_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = string.Empty,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(() => new EventValidationException("Название мероприятия не может быть пустым"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Название мероприятия не может быть пустым");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOFromDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(2),
            TotalSeats = 5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата начала мероприятия должна быть заполнена"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата начала мероприятия должна быть заполнена");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOToDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue,
            TotalSeats = 5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата конца мероприятия должна быть заполнена"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
            .ThrowAsync<EventValidationException>()
            .WithMessage("Дата конца мероприятия должна быть заполнена");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOFromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOZeroTotalSeats_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 0,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество мест должно быть больше 0"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество мест должно быть больше 0");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTONegativeTotalSeats_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = -5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество мест должно быть больше 0"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество мест должно быть больше 0");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTONegativeAvailableSeats_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = -5,
            AvailableSeats = 5
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество мест должно быть больше 0"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество мест должно быть больше 0");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public async Task UpdateEvent_InvalidDTOAvailableSeatsIsBiggerThanTotalSeats_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventInfoDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
            TotalSeats = 5,
            AvailableSeats = 10
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
            .Throws(new EventValidationException("Количество доступных мест не может быть больше мест всего"));

        //Act
        var result = async () => await _eventService.UpdateEventAsync(evntId, evnt);

        //Assert
        await result.Should()
                .ThrowAsync<EventValidationException>()
                .WithMessage("Количество доступных мест не может быть больше мест всего");
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("DeleteEvent", "Success")]
    public async Task DeleteEvent_ExistingId_ShouldDeleteEvent()
    {
        //Arrange
        var evnt = _events.First();

        //Act
        var result = async () => await _eventService.DeleteEventAsync(evnt.Id);

        //Assert
        await result.Should().NotThrowAsync<EventException>();
    }

    [Fact]
    [Trait("DeleteEvent", "Success")]
    public async Task DeleteEvent_IdNotExist_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();

        //Act
        var result = () => _eventService.DeleteEventAsync(evntId);

        // Assert
        await result.Should()
                .ThrowAsync<EventNotFoundException>()
                .WithMessage($"Мероприятие с id {evntId} не найдено");
    }
}
