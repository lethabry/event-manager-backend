#nullable disable
using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

public class Event
{
    private readonly object _reverseLocker = new object();
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; private set; }

    public static Event Create(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Название должно быть обязательным");
        }
        if (startAt >= endAt)
        {
            throw new ValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }
        if (totalSeats <= 0)
        {
            throw new ValidationException("Количество мест должно быть больше 0");
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

    public static Event Create(string title, DateTime startAt, DateTime endAt, int totalSeats, int availableSeats, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Название должно быть обязательным");
        }
        if (startAt >= endAt)
        {
            throw new ValidationException("Дата и время начала мероприятия должна быть раньше, чем дата и время окончания мероприятия");
        }
        if (totalSeats <= 0)
        {
            throw new ValidationException("Количество мест должно быть больше 0");
        }
        if (availableSeats < 0)
        {
            throw new ValidationException("Количество свободных мест должно быть не меньше 0");
        }
        if (availableSeats > totalSeats)
        {
            throw new ValidationException("Количество доступных мест не может быть больше мест всего");
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

public class CreateEventDTO
{
    [Required(ErrorMessage = "Название мероприятия обязательно к заполнению")]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата и время начала мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "Дата и время окончания мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }

    [Required(ErrorMessage = "Количество мест обязательно к заполнению")]
    [Range(1, Int32.MaxValue, ErrorMessage = "Количество мест должно быть больше 0")]
    public int TotalSeats { get; set; }
}

public class EventInfoDTO
{
    [Required(ErrorMessage = "Название мероприятия обязательно к заполнению")]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "Дата и время начала мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "Дата и время окончания мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }

    [Required(ErrorMessage = "Количество мест обязательно к заполнению")]
    [Range(1, Int32.MaxValue, ErrorMessage = "Количество мест должно быть больше 0")]
    public int TotalSeats { get; set; }

    [Required(ErrorMessage = "Количество свободных мест обязательно к заполнению")]
    [Range(0, Int32.MaxValue, ErrorMessage = "Количество свободных мест должно быть не меньше 0")]
    public int AvailableSeats { get; set; }
}
