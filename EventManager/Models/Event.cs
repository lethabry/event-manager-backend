using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventManager.Models;

public class Event
{
    public Guid Id { get; init; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
}

public class EventDTO
{
    [JsonRequired]
    [Required(ErrorMessage = "Название мероприятия обязательно к заполнению")]
    public string Title { get; set; }

    public string? Description { get; set; }

    [JsonRequired]
    [Required(ErrorMessage = "Дата и время начала мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [JsonRequired]
    [Required(ErrorMessage = "Дата и время окончания мероприятия обязательно к заполнению")]
    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }
}