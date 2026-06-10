using System.Collections.Concurrent;
using System.Net;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.BookingService;
using EventManager.Services.EventService;
using FluentAssertions;
using Moq;
using Xunit.Abstractions;

namespace EventManager.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly IBookingService _bookingService;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Event _event;

    private readonly ITestOutputHelper _output;

    public BookingServiceTests(ITestOutputHelper output)
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockEventService = new Mock<IEventService>();
        _bookingService = new BookingService(_mockBookingRepository.Object, _mockEventService.Object);
        _event = Event.Create("Премьера: 'Дюна: Часть вторая' (IMAX)", new DateTime(2026, 4, 22, 19, 0, 0), new DateTime(2026, 4, 22, 22, 15, 0), 5, "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами. ");

        _output = output;
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

        _mockEventService.Setup((e) => e.GetEventById(_event.Id)).Returns(_event);
        _mockBookingRepository.Setup((b) => b.CreateBookingAsync(_event.Id)).Returns(booking);
        _mockEventService.Setup(s => s.UpdateEvent(_event.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((_, dto) => capturedUpdateDto = dto)
            .Returns(evt);

        //Act
        var result = await _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        capturedUpdateDto.AvailableSeats.Should().Be(4);
        result.Should().BeEquivalentTo(bookingDTO);
        _mockEventService.Verify((e) => e.GetEventById(_event.Id), Times.Once);
        _mockEventService.Verify(s => s.UpdateEvent(_event.Id, It.IsAny<EventInfoDTO>()), Times.Once);
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(_event.Id), Times.Once);
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

        _mockEventService.Setup(s => s.GetEventById(evt.Id))
            .Returns(evt);

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(evt.Id))
            .Returns(() =>
            {
                var booking = new Booking(evt.Id);
                bookings.Add(booking);
                return booking;
            });

        _mockEventService.Setup(s => s.UpdateEvent(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((_, dto) =>
            {
                availableSeatsHistory.Add(dto.AvailableSeats);

            })
            .Returns(evt);

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
        _mockEventService.Setup((e) => e.GetEventById(evt.Id)).Returns(evt);

        //Act
        var result = () => _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        result.Should().ThrowAsync<NoAvailableSeatsException>().WithMessage("No available seats for this event").Where(e => e.statusCode == HttpStatusCode.Conflict);
        _mockEventService.Verify((e) => e.GetEventById(_event.Id), Times.Once);
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(_event.Id), Times.Never);
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
        _mockEventService.SetupSequence((e) => e.GetEventById(_event.Id)).Returns(_event).Returns(_event);
        _mockBookingRepository.SetupSequence((b) => b.CreateBookingAsync(_event.Id))
            .Returns(firstBooking)
            .Returns(secondBooking);

        //Act
        var firstResult = await _bookingService.CreateBookingAsync(_event.Id);
        var secondResult = await _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        firstResult.Should().BeEquivalentTo(firstBookingDTO);
        secondResult.Should().BeEquivalentTo(secondBookingDTO);
        firstResult.Should().NotBeSameAs(secondResult);
        _mockEventService.Verify((e) => e.GetEventById(_event.Id), Times.Exactly(2));
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(_event.Id), Times.Exactly(2));
    }

    [Fact]
    [Trait("CreateBooking", "Error")]
    public void CreateBooking_NotExistEventId_ShouldReturnError()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockEventService.Setup((e) => e.GetEventById(id))
            .Throws(new EventException(HttpStatusCode.NotFound, $"Мероприятие с id {id} не найдено"));

        //Act
        var result = () => _bookingService.CreateBookingAsync(id);

        //Assert
        result.Should()
            .ThrowAsync<EventException>()
            .WithMessage($"Мероприятие с id {id} не найдено")
            .Where(e => e.statusCode == HttpStatusCode.NotFound);
        _mockEventService.Verify((e) => e.GetEventById(id), Times.Once);
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(id), Times.Never);
    }


    [Fact]
    [Trait("CreateBooking", "Error")]
    public void CreateBooking_DeletedEventId_ShouldReturnError()
    {
        // Arrange
        var delEvtId = Guid.NewGuid();
        _mockEventService.Setup((e) => e.GetEventById(delEvtId))
            .Throws(new EventException(HttpStatusCode.NotFound,
                $"Мероприятие с id {delEvtId} не найдено"));

        //Act
        var result = () => _bookingService.CreateBookingAsync(delEvtId);

        //Assert
        result.Should()
            .ThrowAsync<EventException>()
            .WithMessage($"Мероприятие с id {delEvtId} не найдено")
            .Where(e => e.statusCode == HttpStatusCode.NotFound);
        _mockEventService.Verify((e) => e.GetEventById(delEvtId), Times.Once);
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(delEvtId), Times.Never);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_ExistBooking_ShouldReturnBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        var bookingDTO = new BookingDTO(booking);
        _mockBookingRepository.Setup((b) => b.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        _mockBookingRepository.Verify((b) => b.GetBookingByIdAsync(booking.Id), Times.Once);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_JustCreatedBooking_ShouldReturnPendingBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        var bookingDTO = new BookingDTO(booking);
        _mockBookingRepository.Setup((b) => b.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("pending");
        result.ProcessedAt.Should().BeNull();
        _mockBookingRepository.Verify((b) => b.GetBookingByIdAsync(booking.Id), Times.Once);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_UpdateBookingStatus_ShouldReturnConfirmedBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        booking.Confirm();
        var bookingDTO = new BookingDTO(booking);
        _mockBookingRepository.Setup((b) => b.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("confirmed");
        result.ProcessedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _mockBookingRepository.Verify((b) => b.GetBookingByIdAsync(booking.Id), Times.Once);
    }

    [Fact]
    [Trait("GetBookingById", "Success")]
    public async Task GetBookingById_UpdateBookingStatus_ShouldReturnRejectedBooking()
    {
        //Arrange
        var booking = new Booking(_event.Id);
        booking.Reject();
        var bookingDTO = new BookingDTO(booking);
        _mockBookingRepository.Setup((b) => b.GetBookingByIdAsync(booking.Id)).ReturnsAsync(booking);

        //Act
        var result = await _bookingService.GetBookingByIdAsync(booking.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        result.Status.Should().Be("rejected");
        result.ProcessedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _mockBookingRepository.Verify((b) => b.GetBookingByIdAsync(booking.Id), Times.Once);
    }

    [Fact]
    [Trait("GetBookingById", "Error")]
    public async Task GetBookingById_NotExistBookingID_ShouldThrowError()
    {
        //Arrange
        var bookingId = Guid.NewGuid();
        _mockBookingRepository.Setup((b) => b.GetBookingByIdAsync(bookingId))
            .ThrowsAsync(new BookingException(HttpStatusCode.NotFound,
                $"Бронирование с id {bookingId} не найдено"));

        //Act
        var result = () => _bookingService.GetBookingByIdAsync(bookingId);

        //Assert
        result.Should()
            .ThrowAsync<BookingException>()
            .WithMessage($"Бронирование с id {bookingId} не найдено")
            .Where((b) => b.statusCode == HttpStatusCode.NotFound);
        _mockBookingRepository.Verify((b) => b.GetBookingByIdAsync(bookingId), Times.Once);
    }

    [Fact]
    [Trait("BookingStatus", "Confirm")]
    public void ConfirmBooking_ShouldSetStatusToConfirmedAndSetProcessedAt()
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
    public void RejectBooking_ShouldSetStatusToRejectedAndSetProcessedAt()
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
    public void RejectBooking_AndReleaseSeats_ShouldRestoreAvailableSeats()
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
    public void RejectBooking_AndReleaseSeats_ShouldAllowNewBookingOnSameSeat()
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

        _mockEventService.Setup(s => s.GetEventById(evt.Id))
            .Returns(() => currentEvent);

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(evt.Id))
            .Returns(() => new Booking(evt.Id));

        _mockEventService.Setup(s => s.UpdateEvent(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((id, dto) =>
            {
                lock (lockObject)
                {
                    currentEvent = Event.Create(evt.Title, evt.StartAt, evt.EndAt, totalSeats, dto.AvailableSeats);
                }
            })
            .Returns(() => currentEvent);

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

        _mockEventService.Setup(s => s.GetEventById(evt.Id))
            .Returns(() => currentEvent);

        _mockBookingRepository.Setup(r => r.CreateBookingAsync(evt.Id))
            .Returns(() => new Booking(evt.Id));

        _mockEventService.Setup(s => s.UpdateEvent(evt.Id, It.IsAny<EventInfoDTO>()))
            .Callback<Guid, EventInfoDTO>((id, dto) =>
            {
                lock (lockObject)
                {
                    currentEvent = Event.Create(evt.Title, evt.StartAt, evt.EndAt, totalSeats, dto.AvailableSeats);
                }
            })
            .Returns(() => currentEvent);

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests).Select(_ => Task.Run(async () =>
        {
            try
            {
                _output.WriteLine($"Before Created booking: {createdBookings.Count} bookings");
                var booking = await _bookingService.CreateBookingAsync(evt.Id);
                createdBookings.Add(booking);
                _output.WriteLine($"Created booking: {createdBookings.Count} bookings");
            }
            catch (Exception exception)
            {
                _output.WriteLine("exception: {0}", exception.Message);
                // Игнорируем
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        createdBookings.Should().HaveCount(totalSeats);
        createdBookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
    }
}
