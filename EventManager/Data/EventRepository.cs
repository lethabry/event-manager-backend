using EventManager.Models;
using EventManager.Services.Interfaces;

namespace EventManager.Data;

public class EventRepository : IEventRepository
{
    private readonly List<Event> _events;

    public EventRepository()
    {
        _events = new()
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
    }

    public List<Event> GetEvents()
    {
        return _events.ToList();
    }

    public Event? GetEventById(Guid id)
    {
        return _events.FirstOrDefault(e => e.Id == id);
    }

    public Event CreateEvent(EventDTO newEvent)
    {
        var updatedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = newEvent.Title,
            Description = string.IsNullOrEmpty(newEvent.Description) ? null : newEvent.Description,
            StartAt = newEvent.StartAt,
            EndAt = newEvent.EndAt,
        };
        _events.Add(updatedEvent);
        return updatedEvent;
    }

    public Event? UpdateEvent(Guid id, EventDTO updatedEvent)
    {
        var existingEvent = _events.FirstOrDefault(e => e.Id == id);
        if (existingEvent != null)
        {
            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description =
                string.IsNullOrEmpty(updatedEvent.Description) ? null : updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;
            return existingEvent;
        }

        return null;
    }

    public bool DeleteEvent(Guid id)
    {
        return _events.RemoveAll((e) => e.Id == id) > 0;
    }
}