using EventManager.Models;
using EventManager.Services.EventService;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Метод для получения всех мероприятий
    /// </summary>
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet]
    public IActionResult GetAll()
    {
        var events = _eventService.GetEvents();
        return Ok(events);
    }

    /// <summary>
    /// Метод для получения мероприятия по id
    /// </summary>
    /// <param name="id">Id мероприятия</param> 
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var existing = _eventService.GetEventById(id);
        return Ok(existing);
    }

    /// <summary>
    /// Метод для создания мероприятия
    /// </summary>
    [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public IActionResult Post([FromBody] EventDTO newEvent)
    {
        var createdEvent = _eventService.CreateEvent(newEvent);
        return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
    }

    /// <summary>
    /// Метод для изменения мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut("{id}")]
    public IActionResult Put(Guid id, [FromBody] EventDTO changedEvent)
    {
        var updatedEvent = _eventService.UpdateEvent(id, changedEvent);
        return Ok(updatedEvent);
    }

    /// <summary>
    /// Метод для удаления мероприятия
    /// </summary>
    /// <param name="id">Id мероприятия</param>
    [ProducesResponseType(typeof(Event), StatusCodes.Status204NoContent)]
    [HttpDelete("{id}")]
    public IActionResult Delete(Guid id)
    {
        _eventService.DeleteEvent(id);
        return NoContent();
    }
}