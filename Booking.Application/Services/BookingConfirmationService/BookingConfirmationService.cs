using Booking.Application.Interfaces;
using Booking.Application.Services.EventClientService;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Common.DTOs;
using EventManager.Contracts.Kafka.Messages.BookingConfirmed;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.Application.Services.BookingConfirmationService;

public class BookingConfirmationService : IBookingConfirmationService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IBookingProducer _bookingProducer;
    private readonly IEventClientService _eventService;

    public BookingConfirmationService(IBookingRepository bookingRepository, IEventClientService eventService, IBookingProducer bookingProducer)
    {
        _bookingRepository = bookingRepository;
        _eventService = eventService;
        _bookingProducer = bookingProducer;
    }

    public async Task ProcessPendingBookingsAsync(CancellationToken ct)
    {
        var pendingBookings = await _bookingRepository.GetBookingsAsync(BookingStatus.Pending);

        foreach (var booking in pendingBookings)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessBookingAsync(booking.Id, ct);
        }
    }

    public async Task ProcessBookingAsync(Guid bookingId, CancellationToken ct)
    {
        EventResponseDTO? evt;
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingNotFoundException(bookingId);
        }

        try
        {
            evt = await _eventService.GetEventByIdAsync(booking.EventId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await RejectBookingAsync(booking, ct);
            return;
        }

        if (evt == null || evt.AvailableSeats == 0)
        {
            await RejectBookingAsync(booking, ct);
            return;
        }

        var activeBookingForUser = await _bookingRepository.GetCountOfActiveBookingsAsync(booking.UserId);

        if (activeBookingForUser > AppConstants.MaxActiveBookings)
        {
            await RejectBookingAsync(booking, ct);
            return;
        }

        if (!booking.Confirm())
        {
            return;
        }

        var savedBooking = await _bookingRepository.UpdateBookingAsync(booking, ct);
        if (savedBooking == null)
        {
            await RejectBookingAsync(booking, ct);
            return;
        }

        var message = new BookingConfirmed()
        {
            BookingId = savedBooking.Id,
            EventId = savedBooking.EventId,
            UserId = savedBooking.UserId,
            AmountSeats = 1,
            ProcessedAt = savedBooking.ProcessedAt ?? DateTime.UtcNow,
        };

        await _bookingProducer.PublishBookingConfirmedMessageAsync(message, ct);
    }

    private async Task RejectBookingAsync(BookingEntity booking,  CancellationToken ct)
    {
        if (!booking.Reject())
        {
            return;
        }

        await _bookingRepository.UpdateBookingAsync(booking, ct);
    }
}
