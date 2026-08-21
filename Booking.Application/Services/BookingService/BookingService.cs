using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Application.Services.EventClientService;
using Booking.Application.Services.UserClientService;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Common.Enums;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Booking.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventClientService _eventClient;
    private readonly IUserClientService _userClientService;
    private readonly IBookingProducer _bookingProducer;

    public BookingService(IBookingRepository bookingRepository,
        IEventClientService eventClient,
        IUserClientService userClientService,
        IBookingProducer bookingProducer)
    {
        _bookingRepository = bookingRepository;
        _eventClient = eventClient;
        _userClientService = userClientService;
        _bookingProducer = bookingProducer;
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
        var evt = await _eventClient.GetEventByIdAsync(eventId);
        if (evt is null)
        {
            throw new EventNotFoundException(eventId);
        }

        if (evt.StartAt <= DateTime.UtcNow)
        {
            throw new BookingPastEventException(eventId);
        }

        var userExists = await _userClientService.CheckIfUserExistAsync(userId);
        if (userExists == null || !userExists.IsUserExist)
        {
            throw new UserNotFoundException($"Пользователь с id {userId} не найден");
        }

        var activeBookings = await _bookingRepository.GetCountOfActiveBookingsAsync(userId);
        if (activeBookings >= AppConstants.MaxActiveBookings)
        {
            throw new ActiveBookingLimitException(userId);
        }
        
        var booking = await _bookingRepository.CreateBookingAsync(eventId, userId, evt.StartAt);
        return new BookingDTO(booking);
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole role)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking is null)
        {
            throw new BookingNotFoundException(bookingId);
        }

        var evt = await _eventClient.GetEventByIdAsync(booking.EventId);
        if (evt == null)
        {
            throw new EventNotFoundException(booking.EventId);
        }

        if (role == UserRole.User && booking.UserId != userId)
        {
            throw new AccessDeniedException("Отменять можно только собственные бронирования");
        }

        var isCanceled = await _bookingRepository.CancelBookingAsync(bookingId);
        
        if (isCanceled)
        {
            var message = new BookingRejected()
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = userId,
                AmountSeats = 1
            };
            await _bookingProducer.PublishBookingRejectedMessageAsync(message);
        }
    }
}
