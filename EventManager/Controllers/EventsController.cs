using EventManager.Models;
using EventManager.Services.BookingService;
using EventManager.Services.EventService;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

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
    [ProducesResponseType(typeof(PaginatedResultDTO<Event>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? title, DateTime? from, DateTime? to, int page = 1,
        int pageSize = 10)
    {
        var events = await _eventService.GetEventsAsync(title, from, to, page, pageSize);
        return Ok(events);
    }

    /// <summary>
    /// Метод для получения мероприятия по id
    /// </summary>
    /// <param name="id">Id мероприятия</param> 
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var existing = await _eventService.GetEventByIdAsync(id);
        return Ok(existing);
    }

    /// <summary>
    /// Метод для создания мероприятия
    /// </summary>
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateEventDTO newEvent)
    {
        var createdEvent = await _eventService.CreateEventAsync(newEvent);
        return CreatedAtAction(nameof(GetById), new { id = createdEvent?.Id }, createdEvent);
    }

    /// <summary>
    /// Метод для изменения мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] EventInfoDTO changedEvent)
    {
        var updatedEvent = await _eventService.UpdateEventAsync(id, changedEvent);
        return Ok(updatedEvent);
    }

    /// <summary>
    /// Метод для удаления мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [ProducesResponseType(typeof(Event), StatusCodes.Status204NoContent)]
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost("{eventId}/book")]
    public async Task<IActionResult> Book(Guid eventId)
    {
        var booking = await _bookingService.CreateBookingAsync(eventId);
        return AcceptedAtAction(
            actionName: nameof(BookingsController.GetById),
            controllerName: "Bookings",
            routeValues: new { id = booking?.Id },
            value: booking
        );
    }
}
