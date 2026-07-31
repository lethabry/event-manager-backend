using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
namespace EventManager.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<BookingDTO?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingNotFoundException(bookingId);
        }

        return new BookingDTO(booking);
    }

    public async Task<BookingDTO?> CreateBookingAsync(Guid eventId)
    {
        var booking = await _bookingRepository.CreateBookingAsync(eventId);
        var bookingDto = new BookingDTO(booking);
        return bookingDto;
    }
}
