using System.Collections.Concurrent;
using EventManager.Models;

namespace EventManager.Data;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> _events;

    public EventRepository()
    {
        var eventList = new[]
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Премьера: 'Дюна: Часть вторая' (IMAX)",
                Description = "Фантастический фильм Дени Вильнёва. Сеанс на русском языке с субтитрами. ",
                StartAt = new DateTime(2026, 4, 22, 19, 0, 0),
                EndAt = new DateTime(2026, 4, 22, 22, 15, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Ночь в кино: Трилогия 'Назад в будущее'",
                Description = "Марафон всех трех частей с перерывом на пиццу. Начало в 23:00. Вход 500₽.",
                StartAt = new DateTime(2026, 4, 25, 23, 0, 0),
                EndAt = new DateTime(2026, 4, 26, 5, 0, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Опера 'Кармен' (Новая сцена)",
                Description = "Дирижер — приглашенный маэстро из Ла Скала. Дресс-код: вечерний.",
                StartAt = new DateTime(2026, 5, 12, 19, 0, 0),
                EndAt = new DateTime(2026, 5, 12, 22, 30, 0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Закрытый показ: 'Мастер и Маргарита' (режиссерская версия)",
                Description = "Только для членов клуба. После показа — Q&A с режиссером.",
                StartAt = new DateTime(2026, 5, 14, 20, 0, 0),
                EndAt = new DateTime(2026, 5, 14, 23, 0, 0)
            }
        };
        _events = new ConcurrentDictionary<Guid, Event>(eventList.ToDictionary((e) => e.Id, (e) => e));
    }

    public IReadOnlyCollection<Event> GetEvents()
    {
        return _events.Values.ToList().AsReadOnly();
    }

    public Event? GetEventById(Guid id)
    {
        return _events.TryGetValue(id, out var value) ? value : null;
    }

    public Event? CreateEvent(EventDTO newEvent)
    {
        var updatedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = newEvent.Title,
            Description = string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description,
            StartAt = newEvent.StartAt,
            EndAt = newEvent.EndAt,
        };
        return _events.TryAdd(updatedEvent.Id, updatedEvent) ? updatedEvent : null;
    }

    public Event? UpdateEvent(Guid id, EventDTO eventDto)
    {
        var updatedEvent = _events.AddOrUpdate(key: id, addValueFactory: _ => null, (key, oldValue) => new Event()
        {
            Id = oldValue.Id,
            Title = eventDto.Title,
            Description = string.IsNullOrEmpty(eventDto.Description) ? null : eventDto.Description,
            StartAt = eventDto.StartAt,
            EndAt = eventDto.EndAt,
        });
        return updatedEvent;
    }

    public bool DeleteEvent(Guid id)
    {
        return _events.TryRemove(id, out _);
    }
}