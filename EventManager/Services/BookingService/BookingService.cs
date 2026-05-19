using System.Net;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;

namespace EventManager.Services.BookingService;

public class BookingService
{
    private IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.NotFound, $"Бронирование с id {bookingId} не найдено");
        }

        return booking;
    }

    public async Task<Booking?> CreateBookingAsync(Guid eventId)
    {
        return await _bookingRepository.CreateBookingAsync(eventId);
    }
}