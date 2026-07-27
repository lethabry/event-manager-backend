using System.Net;
using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
namespace EventManager.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private IBookingRepository _bookingRepository;
    private IEventService _eventService;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingService(IBookingRepository bookingRepository, IEventService eventService)
    {
        _bookingRepository = bookingRepository;
        _eventService = eventService;
    }

    public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.NotFound, $"Бронирование с id {bookingId} не найдено");
        }

        return new BookingDTO(booking);
    }

    public async Task<BookingDTO?> CreateBookingAsync(Guid eventId)
    {
        Booking? booking = null;
        var evt = await _eventService.GetEventByIdAsync(eventId);
        await _processingSemaphore.WaitAsync();
        try
        {
            if (evt != null)
            {
                var isBookingAvailable = evt.TryReserveSeats();
                Console.WriteLine($"IsBookingAvailable: {isBookingAvailable}");
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
                    booking = await _bookingRepository.CreateBookingAsync(eventId);
                    await _eventService.UpdateEventAsync(evt.Id, evtDTO);
                }
                else
                {
                    throw new NoAvailableSeatsException(HttpStatusCode.Conflict, "No available seats for this event");
                }
            }
            if (booking == null)
            {
                throw new BookingException(HttpStatusCode.Conflict, "Не удалось создать бронирование");
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
        return new BookingDTO(booking);
    }
}
