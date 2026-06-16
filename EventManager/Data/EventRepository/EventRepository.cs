using System.Collections.Concurrent;
using EventManager.Models;

namespace EventManager.Data.EventRepository;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> _events;

    public EventRepository()
    {
        var eventList = new[]
        {
            Event.Create("Премьера: 'Дюна: Часть вторая' (IMAX)", new DateTime(2026, 4, 22, 19, 0, 0), new DateTime(2026, 4, 22, 22, 15, 0), 50, "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами."),
            Event.Create("Ночь в кино: Трилогия 'Назад в будущее'", new DateTime(2026, 4, 25, 23, 0, 0), new DateTime(2026, 4, 26, 5, 0, 0), 100, "Марафон всех трех частей с перерывом на пиццу. Начало в 23:00. Вход 500₽."),
            Event.Create("Опера 'Кармен' (Новая сцена)", new DateTime(2026, 5, 12, 19, 0, 0), new DateTime(2026, 5, 12, 22, 30, 0), 150, "Дирижер — приглашенный маэстро из Ла Скала. Дресс-код: вечерний."),
            Event.Create("Закрытый показ: 'Мастер и Маргарита' (режиссерская версия)", new DateTime(2026, 5, 14, 20, 0, 0), new DateTime(2026, 5, 14, 23, 0, 0), 200, "Только для членов клуба. После показа — Q&A с режиссером."),
        };
        _events = new ConcurrentDictionary<Guid, Event>(eventList.ToDictionary((e) => e.Id, (e) => e));
    }

    public IReadOnlyCollection<Event> GetEvents(string? title, DateTime? from, DateTime? to)
    {
        IEnumerable<Event> result = _events.Values.ToList();
        if (!string.IsNullOrEmpty(title))
        {
            result = result.Where((evt) => evt.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        if (from.HasValue)
        {
            result = result.Where((evt) => evt.StartAt >= from.Value);
        }

        if (to.HasValue)
        {
            result = result.Where((evt) => evt.EndAt <= to.Value);
        }

        return result.ToList().AsReadOnly();
    }

    public Event? GetEventById(Guid id)
    {
        return _events.TryGetValue(id, out var value) ? value : null;
    }

    public Event? CreateEvent(CreateEventDTO newEvent)
    {
        var updatedEvent = Event.Create(newEvent.Title, newEvent.StartAt, newEvent.EndAt, newEvent.TotalSeats, string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description);
        return _events.TryAdd(updatedEvent.Id, updatedEvent) ? updatedEvent : null;
    }

    public Event? UpdateEvent(Guid id, EventInfoDTO eventDto)
    {
        while (true)
        {
            _events.TryGetValue(id, out var existingEvent);
            if (existingEvent == null)
            {
                return null;
            }

            var updatedEvent = Event.Create(eventDto.Title, eventDto.StartAt, eventDto.EndAt, eventDto.TotalSeats, eventDto.AvailableSeats, eventDto.Description);
            var result = _events.TryUpdate(id, updatedEvent, existingEvent);
            if (result)
            {
                return updatedEvent;
            }
        }
    }

    public bool DeleteEvent(Guid id)
    {
        return _events.TryRemove(id, out _);
    }
}
