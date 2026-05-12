using System.Net;
using EventManager.Data;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.EventService;
using EventManager.Services.Validation;
using FluentAssertions;
using Moq;
using Xunit.Abstractions;

namespace EventManager.Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IValidationService> _mockValidationService;
    private readonly IEventService _eventService;
    private List<Event> _events;

    public EventServiceTests()
    {
        _events =
        [
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Премьера: 'Дюна: Часть вторая' (IMAX)",
                Description = "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами. ",
                StartAt = new DateTime(2026, 4, 22, 19, 0, 0),
                EndAt = new DateTime(2026, 4, 22, 22, 15, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Ночь в кино: Трилогия 'Назад в будущее'",
                Description = "Марафон всех трех частей с перерывом на пиццу. Начало в 23:00. Вход 500₽.",
                StartAt = new DateTime(2026, 4, 25, 23, 0, 0),
                EndAt = new DateTime(2026, 4, 26, 5, 0, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Опера 'Кармен' (Новая сцена)",
                Description = "Дирижер — приглашенный маэстро из Ла Скала. Дресс-код: вечерний.",
                StartAt = new DateTime(2026, 5, 12, 19, 0, 0),
                EndAt = new DateTime(2026, 5, 12, 22, 30, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Закрытый показ: 'Мастер и Маргарита' (режиссерская версия)",
                Description = "Только для членов клуба. После показа — Q&A с режиссером.",
                StartAt = new DateTime(2026, 5, 14, 20, 0, 0),
                EndAt = new DateTime(2026, 5, 14, 23, 0, 0)
            }
        ];

        _mockEventRepository = new Mock<IEventRepository>();
        _mockValidationService = new Mock<IValidationService>();
        _eventService = new EventService(_mockEventRepository.Object, _mockValidationService.Object);
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
    [Trait("GetEvents", "Successful")]
    public void GetEvents_NoFilters_ShouldReturnAllPaginatedEvents()
    {
        //Arrange
        _mockEventRepository.Setup((rep) => rep.GetEvents(null, null, null))
                            .Returns(_events);
        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = 1,
            currentPageSize = 10,
            result = _events,
            totalAmount = _events.Count
        };

        //Act
        var result = _eventService.GetEvents(null, null, null, 1, 10);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData("Опера")]
    [InlineData("П")]
    [InlineData("IMAX")]
    [Trait("GetEvents", "Successful")]
    public void GetEvents_FilterByTitle_ShouldReturnMatchingEvents(string titleValue)
    {
        //Arrange
        var evt = _events.Where((e) => e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
                         .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(titleValue, null, null))
                            .Returns(evt);

        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = 1,
            currentPageSize = 10,
            result = evt,
            totalAmount = evt.Count
        };

        //Act
        var result = _eventService.GetEvents(titleValue, null, null, 1, 10);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData("asdasd")]
    [InlineData("123")]
    [InlineData("321")]
    [Trait("GetEvents", "Successful")]
    public void GetEvents_FilterByTitle_ShouldReturnEmptyList(string titleValue)
    {
        //Arrange
        var evt = _events.Where((e) => e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
                         .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(titleValue, null, null))
                            .Returns(evt);

        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = 1,
            currentPageSize = 10,
            result = evt,
            totalAmount = evt.Count
        };

        //Act
        var result = _eventService.GetEvents(titleValue, null, null, 1, 10);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(DatesTestData))]
    [Trait("GetEvents", "Successful")]
    public void GetEvents_FilterByDates_ShouldReturnMatchingEvents(DateTime fromDate, DateTime toDate)
    {
        //Arrange
        var evts = _events.Where((e) => e.StartAt >= fromDate && e.EndAt <= toDate)
                          .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(null, fromDate, toDate))
                            .Returns(evts);

        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = 1,
            currentPageSize = 10,
            result = evts,
            totalAmount = evts.Count
        };

        //Act
        var result = _eventService.GetEvents(null, fromDate, toDate, 1, 10);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(FiltersTestData))]
    [Trait("GetEvents", "Successful")]
    public void GetEvent_AllFilters_ShouldReturnMatchingEvents(string titleValue, DateTime fromDate, DateTime toDate)
    {
        //Arrange
        var evts = _events.Where((e) =>
                              e.StartAt >= fromDate && e.EndAt <= toDate &&
                              e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
                          .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(titleValue, fromDate, toDate))
                            .Returns(evts);
        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = 1,
            currentPageSize = 10,
            result = evts,
            totalAmount = evts.Count
        };

        //Act
        var result = _eventService.GetEvents(titleValue, fromDate, toDate, 1, 10);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(4, 1)]
    [Trait("GetEvents", "Successful")]
    public void GetEvent_CustomPaginationWithoutFilters_ShouldReturnMatchingEvents(int page, int pageSize)
    {
        //Arrange
        var evts = _events.Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(null, null, null))
                            .Returns(_events);
        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = page,
            currentPageSize = pageSize,
            result = evts,
            totalAmount = _events.Count()
        };

        //Act
        var result = _eventService.GetEvents(null, null, null, page, pageSize);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Theory]
    [MemberData(nameof(PaginationAndFiltersTestData))]
    [Trait("GetEvents", "Successful")]
    public void GetEvent_CustomPaginationWitFilters_ShouldReturnMatchingEvents(string titleValue, DateTime fromDate,
        DateTime toDate, int page, int pageSize)
    {
        //Arrange
        var evts = _events.Where((e) =>
                              e.StartAt >= fromDate && e.EndAt <= toDate &&
                              e.Title.Contains(titleValue, StringComparison.OrdinalIgnoreCase))
                          .ToList();
        _mockEventRepository.Setup((rep) => rep.GetEvents(titleValue, fromDate, toDate))
                            .Returns(evts);
        var paginatedResult = new PaginatedResultDTO<Event>()
        {
            currentPage = page,
            currentPageSize = pageSize,
            result = evts.Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList(),
            totalAmount = evts.Count()
        };

        //Act
        var result = _eventService.GetEvents(titleValue, fromDate, toDate, page, pageSize);

        //Assert
        result.Should()
              .BeEquivalentTo(paginatedResult);
    }

    [Fact]
    [Trait("GetEvents", "Exception")]
    public void GetEvents_FromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var toDate = new DateTime(2026, 1, 1);
        var fromDate = DateTime.Now;

        //Act
        var result = () => _eventService.GetEvents(null, fromDate, toDate, 1, 10);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата начала мероприятия должны быть раньше даты окончания мероприятия")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.GetEvents(null, null, null), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("GetEvents", "Exception")]
    public void GetEvents_InvalidPage_ShouldThrowError(int page)
    {
        //Act
        var result = () => _eventService.GetEvents(null, null, null, page, 10);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Номер страницы не может быть меньше 1")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.GetEvents(null, null, null), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("GetEvents", "Exception")]
    public void GetEvents_InvalidPageSize_ShouldThrowError(int pageSize)
    {
        //Act
        var result = () => _eventService.GetEvents(null, null, null, 1, pageSize);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Количество элементов не может быть меньше 1")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.GetEvents(null, null, null), Times.Never);
    }

    [Fact]
    [Trait("GetEventsById", "Success")]
    public void GetEventById_IdExist_ShouldReturnEvent()
    {
        //Arrange
        var evt = _events.First();
        _mockEventRepository.Setup((rep) => rep.GetEventById(evt.Id))
                            .Returns(evt);

        //Act
        var result = _eventService.GetEventById(evt.Id);

        //Assert
        result.Should()
              .BeEquivalentTo(evt);
        _mockEventRepository.Verify(x => x.GetEventById(evt.Id), Times.Once);
    }

    [Fact]
    [Trait("GetEventsById", "Error")]
    public void GetEventById_IdNotExist_ShouldThrowError()
    {
        //Arrange
        var randomId = Guid.NewGuid();

        //Act
        var result = () => _eventService.GetEventById(randomId);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage($"Мероприятие с id {randomId} не найдено")
              .Where(e => e.statusCode == HttpStatusCode.NotFound);
        _mockEventRepository.Verify(x => x.GetEventById(randomId), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Success")]
    public void CreateEvent_ValidEventDTO_ShouldReturnCreatedEvent()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            Description = "Новое крутое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };

        var createdEvent = new Event()
        {
            Id = Guid.NewGuid(),
            Title = evnt.Title,
            Description = evnt.Description,
            StartAt = evnt.StartAt,
            EndAt = evnt.EndAt,
        };

        _mockEventRepository.Setup((rep) => rep.CreateEvent(evnt))
                            .Returns(createdEvent);

        //Act
        var result = _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .BeEquivalentTo(createdEvent);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Once);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Success")]
    public void CreateEvent_ValidEventDTOWithoutDescription_ShouldReturnCreatedEvent()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };

        var createdEvent = new Event()
        {
            Id = Guid.NewGuid(),
            Title = evnt.Title,
            StartAt = evnt.StartAt,
            EndAt = evnt.EndAt,
        };

        _mockEventRepository.Setup((rep) => rep.CreateEvent(evnt))
                            .Returns(createdEvent);

        //Act
        var result = _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .BeEquivalentTo(createdEvent);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Once);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public void CreateEvent_InvalidDTOEmptyTitle_ShouldThrowError()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = string.Empty,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(() =>
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Название мероприятия не может быть пустым"));

        //Act
        var result = () => _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Название мероприятия не может быть пустым")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public void CreateEvent_InvalidDTOFromDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата начала мероприятия должна быть заполнена"));

        //Act
        var result = () => _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата начала мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public void CreateEvent_InvalidDTOToDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue,
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата конца мероприятия должна быть заполнена"));

        //Act
        var result = () => _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата конца мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("CreateEvent", "Error")]
    public void CreateEvent_InvalidDTOFromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"));

        //Act
        var result = () => _eventService.CreateEvent(evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.CreateEvent(evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Success")]
    public void UpdateEvent_ValidDTOWithExistingId_ShouldReturnUpdatedEvent()
    {
        //Arrange
        var evnt = _events.First();
        var updateEventDTO = new EventDTO()
        {
            Title = "Новое название",
            Description = "Новое описание",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        var updateEvent = new Event()
        {
            Id = evnt.Id,
            Title = "Новое название",
            Description = "Новое описание",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockEventRepository.Setup((rep) => rep.UpdateEvent(evnt.Id, updateEventDTO))
                            .Returns(updateEvent);

        //Act
        var result = _eventService.UpdateEvent(evnt.Id, updateEventDTO);

        //Assert
        result.Should()
              .BeEquivalentTo(updateEvent);
        _mockEventRepository.Verify(x => x.UpdateEvent(evnt.Id, updateEventDTO), Times.Once);
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Success")]
    public void UpdateEvent_ValidDTOWithoutDescriptionWithExistingId_ShouldReturnUpdatedEvent()
    {
        //Arrange
        var evnt = _events.First();
        var updateEventDTO = new EventDTO()
        {
            Title = "Новое название",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        var updateEvent = new Event()
        {
            Id = evnt.Id,
            Title = "Новое название",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockEventRepository.Setup((rep) => rep.UpdateEvent(evnt.Id, updateEventDTO))
                            .Returns(updateEvent);

        //Act
        var result = _eventService.UpdateEvent(evnt.Id, updateEventDTO);

        //Assert
        result.Should()
              .BeEquivalentTo(updateEvent);
        _mockEventRepository.Verify(x => x.UpdateEvent(evnt.Id, updateEventDTO), Times.Once);
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public void UpdateEvent_ValidDTOWithNotExistingId_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var updateEventDTO = new EventDTO()
        {
            Title = "Новое название",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };

        //Act
        var result = () => _eventService.UpdateEvent(evntId, updateEventDTO);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage($"Мероприятие с id {evntId} не найдено")
              .Where(e => e.statusCode == HttpStatusCode.NotFound);
        _mockEventRepository.Verify(x => x.UpdateEvent(evntId, updateEventDTO), Times.Once);
        _mockValidationService.Verify(x => x.ValidateEventDTO(updateEventDTO), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public void UpdateEvent_InvalidDTOTitleIsEmpty_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventDTO()
        {
            Title = string.Empty,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(() =>
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Название мероприятия не может быть пустым"));

        //Act
        var result = () => _eventService.UpdateEvent(evntId, evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Название мероприятия не может быть пустым")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.UpdateEvent(evntId, evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public void UpdateEvent_InvalidDTOFromDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.MinValue,
            EndAt = DateTime.Now.AddHours(2),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата начала мероприятия должна быть заполнена"));

        //Act
        var result = () => _eventService.UpdateEvent(evntId, evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата начала мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.UpdateEvent(evntId, evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public void UpdateEvent_InvalidDTOToDateIsMinDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.MinValue,
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата конца мероприятия должна быть заполнена"));

        //Act
        var result = () => _eventService.UpdateEvent(evntId, evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата конца мероприятия должна быть заполнена")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.UpdateEvent(evntId, evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("UpdateEvent", "Error")]
    public void UpdateEvent_InvalidDTOFromDateBiggerThanToDate_ShouldThrowError()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        var evnt = new EventDTO()
        {
            Title = "Новое мероприятие",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(-1),
        };
        _mockValidationService.Setup((validation) => validation.ValidateEventDTO(evnt))
                              .Throws(
                                  new EventException(HttpStatusCode.BadRequest,
                                      "Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия"));

        //Act
        var result = () => _eventService.UpdateEvent(evntId, evnt);

        //Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия")
              .Where(e => e.statusCode == HttpStatusCode.BadRequest);
        _mockEventRepository.Verify(x => x.UpdateEvent(evntId, evnt), Times.Never);
        _mockValidationService.Verify(x => x.ValidateEventDTO(evnt), Times.Once);
    }

    [Fact]
    [Trait("DeleteEvent", "Success")]
    public void DeleteEvent_ExistingId_ShouldDeleteEvent()
    {
        //Arrange
        var evnt = _events.First();
        _mockEventRepository.Setup(x => x.GetEventById(evnt.Id))
                            .Returns(evnt);
        _mockEventRepository.Setup(x => x.DeleteEvent(evnt.Id))
                            .Returns(true);

        //Act
        _eventService.DeleteEvent(evnt.Id);

        // Assert
        _mockEventRepository.Verify(x => x.GetEventById(evnt.Id), Times.Once);
        _mockEventRepository.Verify(x => x.DeleteEvent(evnt.Id), Times.Once);
    }

    [Fact]
    [Trait("DeleteEvent", "Success")]
    public void DeleteEvent_IdNotExist_ShouldDeleteEvent()
    {
        //Arrange
        var evntId = Guid.NewGuid();
        _mockEventRepository.Setup(x => x.GetEventById(evntId))
                            .Returns((Event?)null);

        //Act
        var result = () => _eventService.DeleteEvent(evntId);

        // Assert
        result.Should()
              .Throw<EventException>()
              .WithMessage($"Мероприятие с id {evntId} не найдено")
              .Where(e => e.statusCode == HttpStatusCode.NotFound);
        _mockEventRepository.Verify(x => x.GetEventById(evntId), Times.Once);
        _mockEventRepository.Verify(x => x.DeleteEvent(evntId), Times.Never);
    }
}