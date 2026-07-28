using EventManager.Application.DTOs;
using EventManager.Application.Interfaces;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;

namespace EventManager.Application.Services.BookingConfirmationService;

public class BookingConfirmationService : IBookingConfirmationService
{
    private readonly IEventService _eventService;
    private readonly IBookingRepository _bookingRepository;

    public BookingConfirmationService(IEventService eventService, IBookingRepository bookingRepository)
    {
        _eventService = eventService;
        _bookingRepository = bookingRepository;
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
        Event? evt = null;
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            throw new BookingException(404, $"Не найдено бронирование с id {bookingId}");
        }

        try
        {
            evt = await _eventService.GetEventByIdAsync(booking.EventId);

            if (evt == null)
            {
                booking.Reject();
            }
            else
            {
                booking.Confirm();
            }

            await _bookingRepository.UpdateBookingAsync(booking, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            booking.Reject();
            await _bookingRepository.UpdateBookingAsync(booking, ct);

            if (evt != null)
            {
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

                await _eventService.UpdateEventAsync(evt.Id, updatedEvent);
            }
        }
    }
}