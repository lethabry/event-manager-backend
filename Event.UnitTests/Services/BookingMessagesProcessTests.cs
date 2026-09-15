using Event.Application.Configurations;
using Event.Application.Interfaces;
using Event.Application.Services.BookingMessagesProcess;
using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using EventEntity = Event.Domain.Models.Event;

namespace Event.UnitTests.Services;

public class BookingMessagesProcessTests
{
    private readonly Mock<IEventRepository> _repository = new();
    private readonly Mock<IBookingProducer> _producer = new();
    private readonly Mock<ICacher> _cache = new();
    private readonly EventCacheOptions _cacheOptions = new();
    private readonly BookingMessagesProcess _processor;

    public BookingMessagesProcessTests()
    {
        _processor = new BookingMessagesProcess(Mock.Of<ILogger<BookingMessagesProcess>>(), _repository.Object,
            _producer.Object, _cache.Object, Options.Create(_cacheOptions));
    }

    [Fact]
    public async Task HandleConfirmed_EventHasSeats_ReservesSeats()
    {
        var evt = CreateEvent(5);
        var message = CreateConfirmed(evt.Id);
        _repository.Setup(repository => repository.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _repository.Setup(repository => repository.TryReserveSeatsAsync(evt.Id, 1)).ReturnsAsync(true);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        _repository.Verify(repository => repository.TryReserveSeatsAsync(evt.Id, 1), Times.Once);
        _cache.Verify(cache => cache.TryDeleteDataAsync(GetEventCacheKey(evt.Id)), Times.Once);
        _producer.Verify(producer => producer.PublishBookingRejectedMessageAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleConfirmed_EventMissing_PublishesRejected()
    {
        var message = CreateConfirmed(Guid.NewGuid());
        _repository.Setup(repository => repository.GetEventByIdAsync(message.EventId)).ReturnsAsync((EventEntity?)null);
        _producer.Setup(producer => producer.PublishBookingRejectedMessageAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        _producer.Verify(producer => producer.PublishBookingRejectedMessageAsync(
            It.Is<BookingRejected>(rejected => rejected.BookingId == message.BookingId && rejected.EventId == message.EventId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleConfirmed_NoAvailableSeats_PublishesRejected()
    {
        var evt = CreateEvent(1);
        evt.TryReserveSeats();
        var message = CreateConfirmed(evt.Id);
        _repository.Setup(repository => repository.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _producer.Setup(producer => producer.PublishBookingRejectedMessageAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        _repository.Verify(repository => repository.TryReserveSeatsAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        _producer.Verify(producer => producer.PublishBookingRejectedMessageAsync(It.IsAny<BookingRejected>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleCancelled_ExistingEvent_ReleasesSeats()
    {
        var evt = CreateEvent(5);
        var message = new BookingCancelled
        {
            BookingId = Guid.NewGuid(),
            EventId = evt.Id,
            UserId = Guid.NewGuid(),
            AmountSeats = 1
        };
        _repository.Setup(repository => repository.GetEventByIdAsync(evt.Id)).ReturnsAsync(evt);
        _repository.Setup(repository => repository.TryReleaseSeatsAsync(evt.Id, 1)).ReturnsAsync(true);

        await _processor.HandleProcessAsync(message, CancellationToken.None);

        _repository.Verify(repository => repository.TryReleaseSeatsAsync(evt.Id, 1), Times.Once);
        _cache.Verify(cache => cache.TryDeleteDataAsync(GetEventCacheKey(evt.Id)), Times.Once);
    }

    private string GetEventCacheKey(Guid eventId)
    {
        return $"{_cacheOptions.EventKeyPrefix}:{eventId}";
    }

    private static EventEntity CreateEvent(int totalSeats)
    {
        return EventEntity.Create(
            "Kafka event",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            totalSeats);
    }

    private static BookingConfirmed CreateConfirmed(Guid eventId)
    {
        return new BookingConfirmed
        {
            BookingId = Guid.NewGuid(),
            EventId = eventId,
            UserId = Guid.NewGuid(),
            AmountSeats = 1,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
