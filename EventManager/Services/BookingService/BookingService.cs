using System.Net;
using EventManager.Data.BookingRepository;
using EventManager.Exceptions;
using EventManager.Models;

namespace EventManager.Services.BookingService;

public class BookingService : IBookingService
{
    private IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.NotFound, $"Бронирование с id {bookingId} не найдено");
        }

        return new BookingDTO(booking.Id, booking.EventId, booking.Status, booking.ProcessedAt);
    }

    public async Task<BookingDTO?> CreateBookingAsync(Guid eventId)
    {
        var booking = await _bookingRepository.CreateBookingAsync(eventId);
        if (booking == null)
        {
            throw new BookingException(HttpStatusCode.Conflict, "Не удалось создать бронирование");
        }

        return new BookingDTO(booking.Id, booking.EventId, booking.Status, booking.ProcessedAt);
    }
}