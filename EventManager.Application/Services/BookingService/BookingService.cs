using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
namespace EventManager.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, IUserRepository userRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
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
        var evt = await _eventRepository.GetEventByIdAsync(eventId);
        if (evt is null)
        {
            throw new EventNotFoundException(eventId);
        }

        if (evt.StartAt <= DateTime.UtcNow)
        {
            throw new BookingPastEventException(eventId);
        }

        var userExists = await _userRepository.CheckIfUserExistAsync(userId);
        if (!userExists)
        {
            throw new UserNotFoundException($"Пользователь с id {userId} не найден");
        }

        var activeBookings = await _bookingRepository.GetCountOfActiveBookingsAsync(userId);
        if (activeBookings >= AppConstants.MaxActiveBookings)
        {
            throw new ActiveBookingLimitException(userId);
        }

        var booking = await _bookingRepository.CreateBookingAsync(eventId, userId);
        return new BookingDTO(booking);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking is null)
        {
            throw new BookingNotFoundException(bookingId);
        }

        if (role == UserRole.User && booking.UserId != userId)
        {
            throw new AccessDeniedException("Отменять можно только собственные бронирования");
        }

        await _bookingRepository.CancelBookingAsync(bookingId);
    }
}
