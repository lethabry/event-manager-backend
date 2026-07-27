using EventManager.Application.DTOs;
using EventManager.Application.Services.BookingService;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        return Ok(booking);
    }
}
