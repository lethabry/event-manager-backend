using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
namespace EventManager.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingService(IBookingRepository bookingRepository, IEventService eventService, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(404, $"Бронирование с id {bookingId} не найдено", bookingId);
        }

        return new BookingDTO(booking);
    }

    public async Task<BookingDTO?> CreateBookingAsync(Guid eventId)
    {
        BookingDTO? bookingDto = null;
        var evt = await _eventService.GetEventByIdAsync(eventId);
        await _processingSemaphore.WaitAsync();
        try
        {
            if (evt != null)
            {
                var isBookingAvailable = evt.TryReserveSeats();
                _logger.LogDebug("Booking availability for event {EventId}: {IsBookingAvailable}", eventId, isBookingAvailable);
                if (isBookingAvailable)
                {
                    var evtDTO = new EventInfoDTO()
                    {
                        Title = evt.Title,
                        Description = string.IsNullOrEmpty(evt.Description) ? null : evt.Description,
                        AvailableSeats = evt.AvailableSeats,
                        TotalSeats = evt.TotalSeats,
                        StartAt = evt.StartAt,
                        EndAt = evt.EndAt,
                    };
                    var booking = await _bookingRepository.CreateBookingAsync(eventId);
                    await _eventService.UpdateEventAsync(evt.Id, evtDTO);
                    bookingDto = booking == null ? null : new BookingDTO(booking);
                }
                else
                {
                    throw new NoAvailableSeatsException(409, "No available seats for this event", eventId);
                }
            }
            if (bookingDto == null)
            {
                throw new BookingException(409, "Не удалось создать бронирование");
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
        return bookingDto;
    }
}