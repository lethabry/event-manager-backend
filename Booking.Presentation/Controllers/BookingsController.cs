using System.Security.Claims;
using Booking.Application.DTOs;
using Booking.Application.Services.BookingService;
using Booking.Domain.Exceptions;
using EventManager.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }
    
    /// <summary>
    /// Метод для бронирования мероприятия
    /// </summary>
    /// <param name="eventId">Id мероприятия</param>
    [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost("{eventId}/book")]
    public async Task<IActionResult> Book(Guid eventId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            return NotFound("Идентификатор пользователя не найден");
        }

        var userId = Guid.Parse(userIdClaim.Value);
        var booking = await _bookingService.CreateBookingAsync(eventId, userId);
        return AcceptedAtAction(
            actionName: nameof(BookingsController.GetById),
            controllerName: "Bookings",
            routeValues: new { id = booking?.Id },
            value: booking
        );
    }

    /// <summary>
    /// Метод для получения бронирования
    /// </summary>
    /// <param name="id">Id бронирования</param>.
    [ProducesResponseType(typeof(BookingDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(booking);
    }

    /// <summary>
    /// Метод для отмены бронирования
    /// </summary>
    /// <param name="id">Id бронирования</param>.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (userIdClaim == null || roleClaim == null)
        {
            return NotFound("Идентификатор пользователя не найден");
        }

        if (!Enum.TryParse<UserRole>(roleClaim.Value, true, out var role) || !Enum.IsDefined(role))
        {
            throw new UserValidationException("Не удалось распознать роль пользователя");
        }

        var userId = Guid.Parse(userIdClaim.Value);

        await _bookingService.CancelBookingAsync(id, userId, role);
        return NoContent();
    }
}
