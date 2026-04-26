using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

public class Event
{
    public Guid Id;
    public string Title;
    public string? Description;
    public DateTime StartAt;
    public DateTime EndAt;
}

public class EventDTO
{
    [Required(ErrorMessage = "Название мероприятия обязательно к заполнению")]
    public string Title;

    public string? Description;

    [Required(ErrorMessage = "Дата и время начала мероприятия обязательно к заполнению")] [DataType(DataType.Date)]
    public DateTime StartAt;

    [Required(ErrorMessage = "Дата и время окончания мероприятия обязательно к заполнению")] [DataType(DataType.Date)]
    public DateTime EndAt;
}