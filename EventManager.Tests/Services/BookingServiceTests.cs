using System.Collections.Concurrent;
using System.Net;
using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.BookingService;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories.BookingRepository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace EventManager.Tests.Services;

public class BookingServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Event _event;

    public BookingServiceTests(ITestOutputHelper output)
    {
        _mockEventService = new Mock<IEventService>();

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped(_ => _mockEventService.Object);

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();

        _event = Event.Create("Премьера: 'Дюна: Часть вторая' (IMAX)", new DateTime(2026, 4, 22, 19, 0, 0), new DateTime(2026, 4, 22, 22, 15, 0), 5, "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами. ");
        _dbContext.Events.Add(_event);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    [Trait("CreateBooking", "Success")]
    public async Task CreateBooking_ExistEventId_ShouldReturnBooking()
    {
        // Arrange
        var booking = new Booking(_event.Id);
        var bookingDTO = new BookingDTO(booking);
        var evt = Event.Create("Test", DateTime.Now, DateTime.Now.AddHours(2), 5);
        EventInfoDTO capturedUpdateDto = null;

        _mockEventService.Setup((e) => e.GetEventByIdAsync(_event.Id)).ReturnsAsync(_event);
        _mockEventService.Setup(s => s.UpdateEventAsync(_event.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((_, dto) => capturedUpdateDto = dto)
            .ReturnsAsync(evt);

        //Act
        var result = await _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        capturedUpdateDto.AvailableSeats.Should().Be(4);
        result.Should().BeEquivalentTo(bookingDTO, option => option.Excluding(x => x.Id));
        _mockEventService.Verify((e) => e.GetEventByIdAsync(_event.Id), Times.Once);
        _mockEventService.Verify(s => s.UpdateEventAsync(_event.Id, It.IsAny<EventInfoDTO>()), Times.Once);
    }

    [Fact]
    [Trait("CreateBooking", "Success")]
    public async Task CreateBooking_MultipleBookingsUntilLimit_AllShouldSucceedWithUniqueIds()
    {
        // Arrange
        var evt = Event.Create("Test", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var createdBookings = new List<BookingDTO>();
        var availableSeatsHistory = new List<int>();
        var bookings = new List<Booking>();
        var booking = new Booking(evt.Id);
        bookings.Add(booking);

        _mockEventService.Setup(s => s.GetEventByIdAsync(evt.Id))
            .ReturnsAsync(evt);

        _mockEventService.Setup(s => s.UpdateEventAsync(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((_, dto) =>
            {
                availableSeatsHistory.Add(dto.AvailableSeats);

            })
            .ReturnsAsync(evt);

        // Act
        for (int i = 0; i < 4; i++)
        {
            var b = await _bookingService.CreateBookingAsync(evt.Id);
            createdBookings.Add(b);
        }

        // Assert
        createdBookings.Should().HaveCount(4);
        createdBookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        availableSeatsHistory.Should().Equal(4, 3, 2, 1);
    }

    [Fact]
    [Trait("CreateBooking", "Error")]
    public async Task CreateBooking_ExistEventId_ShouldThrowsNoAvailableSeatsException()
    {
        // Arrange
        var evt = Event.Create("Test", DateTime.Now, DateTime.Now.AddHours(2), 5, 0);
        _mockEventService.Setup((e) => e.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);

        //Act
        var result = () => _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        result.Should().ThrowAsync<NoAvailableSeatsException>().WithMessage("No available seats for this event").Where(e => e.statusCode == 409);
        _mockEventService.Verify((e) => e.GetEventByIdAsync(_event.Id), Times.Once);
    }

    [Fact]
    [Trait("CreateBooking", "Success")]
    public async Task CreateBooking_FewBookingOnSingleEvent_ShouldReturnBooking()
    {
        // Arrange
        var firstBooking = new Booking(_event.Id);
        var secondBooking = new Booking(_event.Id);
        var firstBookingDTO = new BookingDTO(firstBooking);
        var secondBookingDTO = new BookingDTO(secondBooking);
        _mockEventService.SetupSequence((e) => e.GetEventByIdAsync(_event.Id)).ReturnsAsync(_event).ReturnsAsync(_event);

        //Act
        var firstResult = await _bookingService.CreateBookingAsync(_event.Id);
        var secondResult = await _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        firstResult.Should().BeEquivalentTo(firstBookingDTO, option => option.Excluding(x => x.Id));
        secondResult.Should().BeEquivalentTo(secondBookingDTO, option => option.Excluding(x => x.Id));
        firstResult.Should().NotBeSameAs(secondResult);
        _mockEventService.Verify((e) => e.GetEventByIdAsync(_event.Id), Times.Exactly(2));
    }

    [Fact]
    [Trait("CreateBooking", "Error")]
    public async Task CreateBooking_NotExistEventId_ShouldReturnError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockEventService.Setup((e) => e.GetEventByIdAsync(id))
            .Throws(new EventException(404, $"Мероприятие с id {id} не найдено"));

        //Act
        var result = () => _bookingService.CreateBookingAsync(id);

        //Assert
        result.Should()
            .ThrowAsync<EventException>()
            .WithMessage($"Мероприятие с id {id} не найдено")
            .Where(e => e.statusCode == 404);
        _mockEventService.Verify((e) => e.GetEventByIdAsync(id), Times.Once);
    }


    [Fact]
    [Trait("CreateBooking", "Error")]
    public async Task CreateBooking_DeletedEventId_ShouldReturnError()
    {
        // Arrange
        var delEvtId = Guid.NewGuid();
        _mockEventService.Setup((e) => e.GetEventByIdAsync(delEvtId))
            .Throws(new EventException(404,
                $"Мероприятие с id {delEvtId} не найдено"));

        //Act
        var result = () => _bookingService.CreateBookingAsync(delEvtId);

        //Assert
        result.Should()
            .ThrowAsync<EventException>()
            .WithMessage($"Мероприятие с id {delEvtId} не найдено")
            .Where(e => e.statusCode == 404);
        _mockEventService.Verify((e) => e.GetEventByIdAsync(delEvtId), Times.Once);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_ExistBooking_ShouldReturnBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();
        var bookingDTO = new BookingDTO(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_JustCreatedBooking_ShouldReturnPendingBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();
        var bookingDTO = new BookingDTO(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("Pending");
        result.ProcessedAt.Should().BeNull();
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_UpdateBookingStatus_ShouldReturnConfirmedBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        booking.Confirm();
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();
        var bookingDTO = new BookingDTO(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("Confirmed");
        result.ProcessedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_UpdateBookingStatus_ShouldReturnRejectedBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        booking.Reject();
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();
        var bookingDTO = new BookingDTO(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("Rejected");
        result.ProcessedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("GetBookingById", "Error")]
    public async Task GetBookingById_NotExistBookingID_ShouldThrowError()
    {
        //Arrange
        var bookingId = Guid.NewGuid();

        //Act
        var result = () => _bookingService.GetBookingByIdAsync(bookingId);

        //Assert
        result.Should()
            .ThrowAsync<BookingException>()
            .WithMessage($"Бронирование с id {bookingId} не найдено")
            .Where((b) => b.statusCode == 404);
    }

    [Fact]
    [Trait("BookingStatus", "Confirm")]
    public async Task ConfirmBooking_ShouldSetStatusToConfirmedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(_event.Id);

        // Act
        booking.Confirm();

        // Assert
        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("BookingStatus", "Reject")]
    public async Task RejectBooking_ShouldSetStatusToRejectedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(_event.Id);

        // Act
        booking.Reject();

        // Assert
        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().NotBeNull();
        booking.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [Trait("BookingStatus", "Reject")]
    public async Task RejectBooking_AndReleaseSeats_ShouldRestoreAvailableSeats()
    {
        // Arrange
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), 5);
        var initialSeats = evt.AvailableSeats;

        // Act
        evt.TryReserveSeats();
        evt.AvailableSeats.Should().Be(initialSeats - 1);
        evt.ReleaseSeats();

        // Assert
        evt.AvailableSeats.Should().Be(initialSeats);
    }

    [Fact]
    [Trait("BookingStatus", "Reject")]
    public async Task RejectBooking_AndReleaseSeats_ShouldAllowNewBookingOnSameSeat()
    {
        // Arrange
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), 5);

        // Act
        for (int i = 0; i < 5; i++)
        {
            evt.TryReserveSeats();
        }
        evt.AvailableSeats.Should().Be(0);
        evt.ReleaseSeats();
        var canBookAgain = evt.TryReserveSeats();

        // Assert
        canBookAgain.Should().BeTrue();
        evt.AvailableSeats.Should().Be(0);
    }

    [Fact]
    [Trait("Concurrency", "Overbooking")]
    public async Task CreateBooking_ConcurrentRequests_ShouldNotOverbook()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), totalSeats);
        var currentEvent = evt;
        var successCount = 0;
        var exceptionCount = 0;
        var lockObject = new object();

        _mockEventService.Setup(s => s.GetEventByIdAsync(evt.Id))
            .ReturnsAsync(() => currentEvent);

        _mockEventService.Setup(s => s.UpdateEventAsync(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((id, dto) =>
            {
                lock (lockObject)
                {
                    currentEvent = Event.Create(evt.Title, evt.StartAt, evt.EndAt, totalSeats, dto.AvailableSeats);
                }
            })
            .ReturnsAsync(() => currentEvent);

        // Act
        var tasks = Enumerable.Range(1, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
                await _bookingService.CreateBookingAsync(evt.Id);
                Interlocked.Increment(ref successCount);
            }
            catch (NoAvailableSeatsException)
            {
                Interlocked.Increment(ref exceptionCount);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        successCount.Should().Be(totalSeats);
        exceptionCount.Should().Be(concurrentRequests - totalSeats);
        currentEvent.AvailableSeats.Should().Be(0);
    }

    [Fact]
    [Trait("Concurrency", "UniqueIds")]
    public async Task CreateBooking_ConcurrentRequests_ShouldHaveUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        const int concurrentRequests = 10;
        var evt = Event.Create("Test Event", DateTime.Now, DateTime.Now.AddHours(2), totalSeats);
        var currentEvent = evt;
        var createdBookings = new ConcurrentBag<BookingDTO>();
        var lockObject = new object();

        _mockEventService.Setup(s => s.GetEventByIdAsync(evt.Id))
            .ReturnsAsync(() => currentEvent);

        _mockEventService.Setup(s => s.UpdateEventAsync(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((id, dto) =>
            {
                lock (lockObject)
                {
                    currentEvent = Event.Create(evt.Title, evt.StartAt, evt.EndAt, totalSeats, dto.AvailableSeats);
                }
            })
            .ReturnsAsync(() => currentEvent);

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
                var booking = await _bookingService.CreateBookingAsync(evt.Id);
                createdBookings.Add(booking);
            }
            catch (Exception exception)
            {
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        createdBookings.Should().HaveCount(totalSeats);
        createdBookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
    }
}
