using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
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

    public async Task<BookingDTO?> CreateBookingAsync(Guid eventId, Guid userId)
    {
        var booking = await _bookingRepository.CreateBookingAsync(eventId, userId);
        var bookingDto = new BookingDTO(booking);
        return bookingDto;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role)
    {
        await _bookingRepository.CancelBookingAsync(bookingId, userId, role);
    }
}
