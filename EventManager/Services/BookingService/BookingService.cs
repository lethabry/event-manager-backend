using System.Net;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.EventService;

namespace EventManager.Services.BookingService;

public class BookingService : IBookingService
{
    private IBookingRepository _bookingRepository;
    private IEventService _eventService;

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
         _eventService.GetEventById(eventId);
        var booking = await _bookingRepository.CreateBookingAsync(eventId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.Conflict, "Не удалось создать бронирование");
        }

        return new BookingDTO(booking);
    }
}