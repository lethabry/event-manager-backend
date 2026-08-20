using Booking.Application.Interfaces;
using Booking.Application.Services.EventClientService;
using Booking.Domain.Common;
using Booking.Domain.Exceptions;
using EventManager.Common.DTOs;
using BookingEntity = Booking.Domain.Models.Booking;

namespace Booking.Application.Services.BookingConfirmationService;

public class BookingConfirmationService : IBookingConfirmationService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventClientService _eventService;

    public BookingConfirmationService(IBookingRepository bookingRepository, IEventClientService eventService)
    {
        _bookingRepository = bookingRepository;
        _eventService = eventService;
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
            // Запрос идет по api EventClientService
            evt = await _eventService.GetEventByIdAsync(booking.EventId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await RejectBookingAsync(booking, null, ct);
            return;
        }

        if (evt == null)
        {
            await RejectBookingAsync(booking, null, ct);
            return;
        }

        var activeBookingForUser = await _bookingRepository.GetCountOfActiveBookingsAsync(booking.UserId);

        if (activeBookingForUser > AppConstants.MaxActiveBookings)
        {
            await RejectBookingAsync(booking, evt, ct);
            return;
        }

        if (!booking.Confirm())
        {
            return;
        }

        await _bookingRepository.UpdateBookingAsync(booking, ct);
    }

    private async Task RejectBookingAsync(BookingEntity booking, EventResponseDTO? evt, CancellationToken ct)
    {
        if (!booking.Reject())
        {
            return;
        }

        var updatedBooking = await _bookingRepository.UpdateBookingAsync(booking, ct);
        if (updatedBooking == null)
        {
            return;
        }

        if (evt == null)
        {
            return;
        }


        /* Вот тут должен работать kafka с отправкой сообщения

         evt.ReleaseSeats();
        var updatedEvent = new EventInfoDTO
        {
            Title = evt.Title,
            Description = string.IsNullOrEmpty(evt.Description) ? null : evt.Description,
            StartAt = evt.StartAt,
            EndAt = evt.EndAt,
            TotalSeats = evt.TotalSeats,
            AvailableSeats = evt.AvailableSeats
        };

        await _eventService.UpdateEventAsync(evt.Id, updatedEvent);*/
    }
}
