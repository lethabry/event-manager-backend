using Booking.Application.DTOs;
using Booking.Application.Interfaces;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Common.Enums;
using EventManager.Contracts.Kafka.Messages.BookingCancelled;
using EventManager.Contracts.Kafka.Messages.BookingRejected;
namespace Booking.Application.Services.BookingService;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingProducer _bookingProducer;

    public BookingService(IBookingRepository bookingRepository, IBookingProducer bookingProducer)
    {
        _bookingRepository = bookingRepository;
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
        var previousStatus = booking.Status;
        var isCanceled = await _bookingRepository.CancelBookingAsync(bookingId);

        if (isCanceled && previousStatus is BookingStatus.Confirmed)
        {
            var message = new BookingCancelled()
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                AmountSeats = 1
            };
            await _bookingProducer.PublishBookingCancelledMessageAsync(message);
        }
    }
}
