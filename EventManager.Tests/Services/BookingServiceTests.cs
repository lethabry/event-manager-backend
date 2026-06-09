using System.Net;
using EventManager.Common;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.BookingService;
using EventManager.Services.EventService;
using FluentAssertions;
using Moq;

namespace EventManager.Tests.Services;

public class BookingServiceTests
{
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly IBookingService _bookingService;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Event _event;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockEventService = new Mock<IEventService>();
        _bookingService = new BookingService(_mockBookingRepository.Object, _mockEventService.Object);
        _event = Event.Create("Премьера: 'Дюна: Часть вторая' (IMAX)", new DateTime(2026, 4, 22, 19, 0, 0), new DateTime(2026, 4, 22, 22, 15, 0), 5, "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами. ");
    }

    [Fact]
    [Trait("CreateBooking", "Success")]
    public async Task CreateBooking_ExistEventId_ShouldReturnBooking()
    {
        // Arrange
        var booking = new Booking(_event.Id);
        var bookingDTO = new BookingDTO(booking);
        _mockEventService.Setup((e) => e.GetEventById(_event.Id)).Returns(_event);
        _mockBookingRepository.Setup((b) => b.CreateBookingAsync(_event.Id)).Returns(booking);

        //Act
        var result = await _bookingService.CreateBookingAsync(_event.Id);

        //Assert
        result.Should().BeEquivalentTo(bookingDTO);
        _mockEventService.Verify((e) => e.GetEventById(_event.Id), Times.Once);
        _mockBookingRepository.Verify((b) => b.CreateBookingAsync(_event.Id), Times.Once);
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
}
