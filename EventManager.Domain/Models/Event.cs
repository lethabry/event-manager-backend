#nullable disable
using EventManager.Domain.Exceptions;

namespace EventManager.Domain.Models;

public class Event
{
    private readonly object _reverseLocker = new object();
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public List<Booking> Bookings { get; set; }

    private Event()
    {

    }

    public static Event Create(string title, DateTime startAt, DateTime endAt, int totalSeats, string description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new EventValidationException("Название должно быть обязательным");
        }
        if (startAt >= endAt)
        {
            throw new EventValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }
        if (totalSeats <= 0)
        {
            throw new EventValidationException("Количество мест должно быть больше 0");
        }
        return new Event()
        {
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public static Event Create(string title, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats, string description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new EventValidationException("Название должно быть обязательным");
        }
        if (startAt >= endAt)
        {
            throw new EventValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }
        if (totalSeats <= 0)
        {
            throw new EventValidationException("Количество мест должно быть больше 0");
        }
        if (availableSeats < 0)
        {
            throw new EventValidationException("Количество свободных мест должно быть не меньше 0");
        }
        if (availableSeats > totalSeats)
        {
            throw new EventValidationException("Количество доступных мест не может быть больше мест всего");
        }
        return new Event()
        {
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats
        };
    }

    public bool TryReserveSeats(int count = 1)
    {
        lock (_reverseLocker)
        {
            if (AvailableSeats >= count)
            {
                AvailableSeats -= count;
                return true;
            }
        }
        return false;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }
}
