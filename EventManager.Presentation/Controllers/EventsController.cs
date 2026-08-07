using System.Security.Claims;
using EventManager.Application.DTOs;
using EventManager.Application.Services.BookingService;
using EventManager.Application.Services.EventService;
using EventManager.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    /// <summary>
    /// Метод для получения всех мероприятий
    /// </summary>
    /// <param name="title">Фильтрация по названию</param>>
    /// <param name="from">Фильтрация от конкретной даты мероприятия</param>>
    /// <param name="to">Фильтрация до конкретной даты мероприятия</param>>
    /// <param name="page">Номер страницы</param>>
    /// <param name="pageSize">Количество элементов в странице</param>>
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResponseDTO<EventResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? title, DateTime? from, DateTime? to, int page = 1,
        int pageSize = 10)
    {
        var events = await _eventService.GetEventsAsync(title, from, to, page, pageSize);
        return Ok(MapToPaginatedResponse(events));
    }

    /// <summary>
    /// Метод для получения мероприятия по id
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var existing = await _eventService.GetEventByIdAsync(id);
        return Ok(MapToResponse(existing));
    }

    /// <summary>
    /// Метод для создания мероприятия
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateEventDTO newEvent)
    {
        var createdEvent = await _eventService.CreateEventAsync(newEvent);
        return CreatedAtAction(nameof(GetById), new { id = createdEvent?.Id }, MapToResponse(createdEvent));
    }

    /// <summary>
    /// Метод для изменения мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    /// <param name="changedEvent">Данные для изменения мероприятия</param>
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] EventInfoDTO changedEvent)
    {
        var updatedEvent = await _eventService.UpdateEventAsync(id, changedEvent);
        return Ok(MapToResponse(updatedEvent));
    }

    /// <summary>
    /// Метод для удаления мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _eventService.DeleteEventAsync(id);
        return NoContent();
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

    private static PaginatedResponseDTO<EventResponseDTO> MapToPaginatedResponse(PaginatedResultDTO<Event> events)
    {
        return new PaginatedResponseDTO<EventResponseDTO>
        {
            TotalAmount = events.TotalAmount,
            Result = events.Result.Select(evt => MapToResponse(evt)!).ToList(),
            CurrentPage = events.CurrentPage,
            CurrentPageSize = events.CurrentPageSize
        };
    }

    private static EventResponseDTO? MapToResponse(Event? evt)
    {
        if (evt == null)
        {
            return null;
        }

        return new EventResponseDTO
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            StartAt = evt.StartAt,
            EndAt = evt.EndAt,
            TotalSeats = evt.TotalSeats,
            AvailableSeats = evt.AvailableSeats
        };
    }
}
