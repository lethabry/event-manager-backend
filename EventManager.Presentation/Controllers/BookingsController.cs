using System.Security.Claims;
using EventManager.Application.DTOs;
using EventManager.Application.Services.BookingService;
using EventManager.Domain.Common;
using EventManager.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

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
