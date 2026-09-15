using Event.Domain.Exceptions;

namespace Event.Domain.Models;

public sealed class Event
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }

    private Event()
    {

    }

    public static Event Create(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        return Reconstruct(title, startAt, endAt, totalSeats, totalSeats, description);
    }

    public static Event Reconstruct(string title, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats,
        string? description = null)
    {
        return Reconstruct(Guid.NewGuid(), title, startAt, endAt, totalSeats, availableSeats, description);
    }

    public static Event Reconstruct(Guid id, string title, DateTime startAt, DateTime endAt, int totalSeats,
        int availableSeats, string? description = null)
    {
        var evt = new Event { Id = id };
        evt.Update(title, startAt, endAt, totalSeats, availableSeats, description);
        return evt;
    }

    public void Update(string title, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats,
        string? description = null)
    {
        Validate(title, startAt, endAt, totalSeats, availableSeats);
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = availableSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;
    }

    private static void Validate(string title, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats)
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
    }
}
